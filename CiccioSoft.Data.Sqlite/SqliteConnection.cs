// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using CiccioSoft.Data.Sqlite.Properties;
using CiccioSoft.Sqlite.Interop;
using static CiccioSoft.Sqlite.Interop.Native.sqlite3;

namespace CiccioSoft.Data.Sqlite;

public class SqliteConnection : DbConnection
{
    private readonly object _syncRoot = new();
    private string _connectionString = string.Empty;
    private ConnectionState _state = ConnectionState.Closed;
    private SqliteSession? _session;
    private bool _hasActiveTransaction;
    private SqliteConnectionStringBuilder _settings = new();

    public SqliteConnection() { }

    public SqliteConnection(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public override string ConnectionString
    {
        get => _connectionString;
        set
        {
            lock (_syncRoot)
            {
                if (_state != ConnectionState.Closed) throw new InvalidOperationException("Connection must be closed.");
                _connectionString = value ?? string.Empty;
                _settings = new SqliteConnectionStringBuilder { ConnectionString = _connectionString };
            }
        }
    }

    public override string Database => "main";

    public override string DataSource => _settings.DataSource;

    public override string ServerVersion => "3";

    public override ConnectionState State
    {
        get
        {
            lock (_syncRoot)
            {
                return _state;
            }
        }
    }

    public override void ChangeDatabase(string databaseName)
    {
        throw new NotSupportedException("SQLite does not support changing active database through ADO.NET connection.");
    }

    public override void Close()
    {
        SqliteSession? session;
        bool pooling;
        string connectionString;
        lock (_syncRoot)
        {
            if (_state == ConnectionState.Closed)
                return;

            session = Interlocked.Exchange(ref _session, null);
            pooling = IsPoolingEnabled();
            connectionString = _connectionString;
            _state = ConnectionState.Closed;
            _hasActiveTransaction = false;
        }

        if (session is null)
            return;

        session.Gate.Wait();
        try
        {
            if (pooling)
            {
                session.Gate.Release();
                SqliteConnectionPool.Return(connectionString, session);
                return;
            }
        }
        finally
        {
            if (!pooling)
            {
                session.Dispose();
            }
        }
    }

    public override void Open()
    {
        lock (_syncRoot)
        {
            if (_state != ConnectionState.Closed)
                return;

            if (string.IsNullOrWhiteSpace(_settings.DataSource))
                throw new InvalidOperationException("Data Source is required.");

            try
            {
                bool pooling = IsPoolingEnabled();
                SqliteSession session = pooling
                    ? SqliteConnectionPool.Rent(_connectionString, _settings.DataSource, _settings.MaxPoolSize, GetOpenFlags())
                    : new SqliteSession(Sqlite3.Open(_settings.DataSource, GetOpenFlags()));

                ApplyConnectionSettings(session.Native);

                _session = session;
                _state = ConnectionState.Open;
            }
            catch (SqliteInteropException ex)
            {
                throw new SqliteException(ex.Message, ex.BaseErrorCode, ex.ExtendedErrorCode, ex);
            }
        }
    }

    public override Task OpenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Open();
        return Task.CompletedTask;
    }

    public override DataTable GetSchema()
    {
        return GetSchema(DbMetaDataCollectionNames.MetaDataCollections);
    }

    public override DataTable GetSchema(string collectionName)
    {
        if (!TryNormalizeCollectionName(collectionName, out string normalizedCollectionName))
        {
            throw new ArgumentException(Resources.UnknownCollection(collectionName));
        }

        if (string.Equals(normalizedCollectionName, DbMetaDataCollectionNames.MetaDataCollections, StringComparison.Ordinal))
        {
            return CreateMetaDataCollectionsTable();
        }

        if (string.Equals(normalizedCollectionName, DbMetaDataCollectionNames.ReservedWords, StringComparison.Ordinal))
        {
            return CreateReservedWordsTable();
        }

        throw new ArgumentException(Resources.UnknownCollection(collectionName));
    }

    public override DataTable GetSchema(string collectionName, string?[]? restrictionValues)
    {
        if (!TryNormalizeCollectionName(collectionName, out string normalizedCollectionName))
        {
            throw new ArgumentException(Resources.UnknownCollection(collectionName));
        }

        if (restrictionValues is { Length: > 0 })
        {
            throw new ArgumentException(Resources.TooManyRestrictions(normalizedCollectionName));
        }

        return GetSchema(normalizedCollectionName);
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        lock (_syncRoot)
        {
            EnsureOpen();
            if (_hasActiveTransaction)
            {
                throw new InvalidOperationException(Resources.ParallelTransactionsNotSupported);
            }

            _hasActiveTransaction = true;
        }

        try
        {
            return new SqliteTransaction(this, isolationLevel);
        }
        catch
        {
            ClearActiveTransaction();
            throw;
        }
    }

    protected override DbCommand CreateDbCommand()
    {
        return new SqliteCommand { Connection = this };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Close();
        base.Dispose(disposing);
    }

    internal SqliteSession GetSession()
    {
        lock (_syncRoot)
        {
            EnsureOpen();
            return _session!;
        }
    }

    internal void EnsureOpen()
    {
        if (_state != ConnectionState.Open || _session is null)
            throw new InvalidOperationException("Connection is not open.");
    }

    internal void ClearActiveTransaction()
    {
        lock (_syncRoot)
        {
            _hasActiveTransaction = false;
        }
    }


    private static bool TryNormalizeCollectionName(string? collectionName, out string normalizedCollectionName)
    {
        if (string.Equals(collectionName, DbMetaDataCollectionNames.MetaDataCollections, StringComparison.OrdinalIgnoreCase))
        {
            normalizedCollectionName = DbMetaDataCollectionNames.MetaDataCollections;
            return true;
        }

        if (string.Equals(collectionName, DbMetaDataCollectionNames.ReservedWords, StringComparison.OrdinalIgnoreCase))
        {
            normalizedCollectionName = DbMetaDataCollectionNames.ReservedWords;
            return true;
        }

        normalizedCollectionName = string.Empty;
        return false;
    }

    private static DataTable CreateMetaDataCollectionsTable()
    {
        var table = new DataTable(DbMetaDataCollectionNames.MetaDataCollections);
        table.Columns.Add(DbMetaDataColumnNames.CollectionName, typeof(string));
        table.Columns.Add(DbMetaDataColumnNames.NumberOfRestrictions, typeof(int));
        table.Columns.Add(DbMetaDataColumnNames.NumberOfIdentifierParts, typeof(int));

        table.Rows.Add(DbMetaDataCollectionNames.MetaDataCollections, 0, 0);
        table.Rows.Add(DbMetaDataCollectionNames.ReservedWords, 0, 0);

        return table;
    }

    private static DataTable CreateReservedWordsTable()
    {
        var table = new DataTable(DbMetaDataCollectionNames.ReservedWords);
        table.Columns.Add(DbMetaDataColumnNames.ReservedWord, typeof(string));
        table.Rows.Add("SELECT");

        return table;
    }

    private bool IsPoolingEnabled()
    {
        return _settings.Pooling;
    }

    private int GetOpenFlags()
    {
        int flags = SQLITE_OPEN_READWRITE | SQLITE_OPEN_CREATE | SQLITE_OPEN_FULLMUTEX;

        if (_settings.DataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            flags |= SQLITE_OPEN_URI;
        }

        return flags;
    }

    private void ApplyConnectionSettings(Sqlite3 native)
    {
        native.SetBusyTimeout(Math.Max(0, _settings.BusyTimeout));

        if (_settings.HasForeignKeys)
        {
            native.Execute($"PRAGMA foreign_keys={(_settings.ForeignKeys == true ? "ON" : "OFF")};");
        }

        if (_settings.HasJournalMode)
        {
            native.Execute($"PRAGMA journal_mode={_settings.JournalMode};");
        }

        if (_settings.HasRecursiveTriggers)
        {
            native.Execute($"PRAGMA recursive_triggers={(_settings.RecursiveTriggers == true ? "ON" : "OFF")};");
        }
    }
}
