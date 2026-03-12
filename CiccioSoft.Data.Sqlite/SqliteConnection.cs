// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CiccioSoft.Data.Sqlite.Properties;
using CiccioSoft.Sqlite.Interop;

namespace CiccioSoft.Data.Sqlite;

public class SqliteConnection : DbConnection
{
    private readonly object _syncRoot = new();
    private string _connectionString = string.Empty;
    private ConnectionState _state = ConnectionState.Closed;
    private SqliteSession? _session;
    private bool _hasActiveTransaction;
    private SqliteTransaction? _activeTransaction;
    private SqliteConnectionStringBuilder _settings = new();
    private string _dataSource = string.Empty;

    public SqliteConnection() { }

    public SqliteConnection(string connectionString)
    {
        ConnectionString = connectionString;
    }

    [DefaultValue("")]
    [SettingsBindableAttribute(true)]
    [RefreshProperties(RefreshProperties.All)]
    [AllowNull]
    public override string ConnectionString
    {
        get => _connectionString;
        set
        {
            lock (_syncRoot)
            {
                if (_state != ConnectionState.Closed) throw new InvalidOperationException(Resources.ConnectionStringRequiresClosedConnection);
                _connectionString = value ?? string.Empty;
                _settings = new SqliteConnectionStringBuilder { ConnectionString = _connectionString };
                _dataSource = _settings.DataSource;
            }
        }
    }

    public override string Database => "main";

    public override string DataSource => _dataSource;

    public override string ServerVersion => "3.0.0";

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

    protected override DbProviderFactory DbProviderFactory => SqliteFactory.Instance;

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
            _activeTransaction = null;
        }

        OnStateChange(new StateChangeEventArgs(ConnectionState.Open, ConnectionState.Closed));

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

            string dataSource = ResolveDataSource();
            SqliteOpenFlags openFlags = GetOpenFlags(dataSource);

            if (_settings.TryGetValue("Password", out object? password)
                && !string.IsNullOrWhiteSpace(Convert.ToString(password)))
            {
                throw new InvalidOperationException(Resources.EncryptionNotSupported("sqlite3"));
            }

            try
            {
                bool pooling = IsPoolingEnabled();
                SqliteSession session = pooling
                    ? SqliteConnectionPool.Rent(_connectionString, dataSource, _settings.MaxPoolSize, openFlags)
                    : new SqliteSession(Sqlite3.Open(dataSource, openFlags));

                ApplyConnectionSettings(session.Native);

                _session = session;
                _dataSource = dataSource;
                _state = ConnectionState.Open;
            }
            catch (SqliteInteropException ex)
            {
                throw new SqliteException(ex.Message, (int)ex.BaseErrorCode, ex.ExtendedErrorCode, ex);
            }

            OnStateChange(new StateChangeEventArgs(ConnectionState.Closed, ConnectionState.Open));
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
        return new SqliteCommand
        {
            Connection = this,
            Transaction = _activeTransaction,
        };
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

    internal void EnsureOpen([CallerMemberName] string? operation = null)
    {
        if (_state != ConnectionState.Open || _session is null)
            throw new InvalidOperationException(Resources.CallRequiresOpenConnection(operation ?? "operation"));
    }

    internal void ClearActiveTransaction()
    {
        lock (_syncRoot)
        {
            _hasActiveTransaction = false;
            _activeTransaction = null;
        }
    }

    internal void SetActiveTransaction(SqliteTransaction transaction)
    {
        lock (_syncRoot)
        {
            _activeTransaction = transaction;
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
        if (!_settings.Pooling)
        {
            return false;
        }

        if (string.Equals(_settings.DataSource, ":memory:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (_settings.TryGetValue("Mode", out object? modeValue)
            && string.Equals(Convert.ToString(modeValue), "Memory", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private SqliteOpenFlags GetOpenFlags(string dataSource)
    {
        SqliteOpenFlags flags = SqliteOpenFlags.ReadWrite | SqliteOpenFlags.Create | SqliteOpenFlags.FullMutex;

        if (_settings.TryGetValue("Mode", out object? modeValue))
        {
            string mode = Convert.ToString(modeValue) ?? string.Empty;
            if (string.Equals(mode, "ReadOnly", StringComparison.OrdinalIgnoreCase))
            {
                flags = SqliteOpenFlags.ReadOnly | SqliteOpenFlags.FullMutex;
            }
            else if (string.Equals(mode, "ReadWrite", StringComparison.OrdinalIgnoreCase))
            {
                flags = SqliteOpenFlags.ReadWrite | SqliteOpenFlags.FullMutex;
            }
            else if (string.Equals(mode, "Memory", StringComparison.OrdinalIgnoreCase))
            {
                flags = SqliteOpenFlags.ReadWrite | SqliteOpenFlags.Create | SqliteOpenFlags.Memory | SqliteOpenFlags.FullMutex;
            }
        }

        if (dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            flags |= SqliteOpenFlags.Uri;
        }

        if (_settings.TryGetValue("Cache", out object? cacheValue))
        {
            string cache = Convert.ToString(cacheValue) ?? string.Empty;
            if (string.Equals(cache, "Shared", StringComparison.OrdinalIgnoreCase))
            {
                flags |= SqliteOpenFlags.SharedCache;
            }
            else if (string.Equals(cache, "Private", StringComparison.OrdinalIgnoreCase))
            {
                flags |= SqliteOpenFlags.PrivateCache;
            }
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

        if (_settings.HasJournalMode && !string.IsNullOrWhiteSpace(_settings.JournalMode))
        {
            native.Execute($"PRAGMA journal_mode={_settings.JournalMode};");
        }

        if (_settings.HasRecursiveTriggers)
        {
            native.Execute($"PRAGMA recursive_triggers={(_settings.RecursiveTriggers == true ? "ON" : "OFF")};");
        }
    }

    private string ResolveDataSource()
    {
        string dataSource = _settings.DataSource;
        if (string.IsNullOrWhiteSpace(dataSource))
        {
            return ":memory:";
        }

        if (dataSource.StartsWith("|DataDirectory|", StringComparison.OrdinalIgnoreCase))
        {
            string baseDirectory = Convert.ToString(AppDomain.CurrentDomain.GetData("DataDirectory")) ?? AppContext.BaseDirectory;
            return Path.Combine(baseDirectory, dataSource[15..]);
        }

        if (dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase) || dataSource == ":memory:")
        {
            return dataSource;
        }

        if (_settings.TryGetValue("Mode", out object? modeValue)
            && string.Equals(Convert.ToString(modeValue), "Memory", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrEmpty(dataSource))
        {
            string cacheSuffix = string.Empty;
            if (_settings.TryGetValue("Cache", out object? cacheValue)
                && string.Equals(Convert.ToString(cacheValue), "Shared", StringComparison.OrdinalIgnoreCase))
            {
                cacheSuffix = "&cache=shared";
            }

            return $"file:{dataSource}?mode=memory{cacheSuffix}";
        }

        return Path.IsPathRooted(dataSource)
            ? dataSource
            : Path.Combine(AppContext.BaseDirectory, dataSource);
    }
}
