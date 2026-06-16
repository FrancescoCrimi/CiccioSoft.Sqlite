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

/// <summary>
/// Represents a connection to a SQLite database.
/// Intelligent defaults: WAL enabled, Foreign Keys ON, Shared Cache for in-memory.
/// True async support with native interrupt.
/// </summary>
public sealed class SqliteConnection : DbConnection
{
    private readonly object _syncRoot = new();
    private string _connectionString = string.Empty;
    private ConnectionState _state = ConnectionState.Closed;
    private SqliteSession? _session;
    private bool _hasActiveTransaction;
    private SqliteTransaction? _activeTransaction;
    private SqliteConnectionStringBuilder _settings = new();
    private string _dataSource = string.Empty;
    private string _writerKey = string.Empty;
    private int? _defaultTimeout;

    public SqliteConnection() { }

    public SqliteConnection(string connectionString)
    {
        ConnectionString = connectionString;
    }

    /// <summary>
    /// Gets the underlying low-level SQLite interop object for advanced/native operations.
    /// </summary>
    public Sqlite3 Interop
    {
        get
        {
            EnsureOpen();
            return _session!.Native;
        }
    }

    /// <summary>
    ///     Gets a handle to underlying database connection.
    /// </summary>
    /// <value>A handle to underlying database connection.</value>
    /// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/interop">Interoperability</seealso>
    public virtual Sqlite3? Handle
        => _session?.Native;

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
                _writerKey = ResolveWriterKey(_connectionString, _dataSource);
            }
        }
    }

    /// <summary>
    ///     Gets the name of the current database. Always 'main'.
    /// </summary>
    /// <value>The name of the current database.</value>
    public override string Database => "main";

    public override string DataSource => _dataSource;

    //TODO: fix DefaultTimeout
    public virtual int DefaultTimeout
    {
        get => _defaultTimeout ?? _settings.DefaultTimeout;
        set => _defaultTimeout = value;
    }


    public override string ServerVersion
    {
        get
        {
            return Sqlite3.LibVersion();
        }
    }

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
                _writerKey = ResolveWriterKey(_connectionString, _dataSource);
                _state = ConnectionState.Open;
            }
            catch (SqliteInteropException ex)
            {
                throw new SqliteException(ex.Message, ex);
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

    /// <summary>
    /// Executes <c>PRAGMA wal_checkpoint(PASSIVE)</c> on the current connection.
    /// </summary>
    public virtual void Checkpoint()
        => Checkpoint(SqliteWalCheckpointMode.Passive);

    /// <summary>
    /// Executes a WAL checkpoint using the requested mode.
    /// </summary>
    public virtual void Checkpoint(SqliteWalCheckpointMode mode, CancellationToken cancellationToken = default)
    {
        using IDisposable writerGate = AcquireWriterGate(cancellationToken);
        ExecuteSessionPragma($"PRAGMA wal_checkpoint({ToCheckpointPragma(mode)});", cancellationToken);
    }

    /// <summary>
    /// Asynchronously executes <c>PRAGMA wal_checkpoint(PASSIVE)</c>.
    /// </summary>
    public virtual Task CheckpointAsync(CancellationToken cancellationToken = default)
        => CheckpointAsync(SqliteWalCheckpointMode.Passive, cancellationToken);

    /// <summary>
    /// Asynchronously executes a WAL checkpoint using the requested mode.
    /// </summary>
    public virtual Task CheckpointAsync(SqliteWalCheckpointMode mode, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Checkpoint(mode, cancellationToken);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes <c>PRAGMA optimize</c> for lightweight planner/statistics maintenance.
    /// </summary>
    public virtual void Optimize(CancellationToken cancellationToken = default)
    {
        using IDisposable writerGate = AcquireWriterGate(cancellationToken);
        ExecuteSessionPragma("PRAGMA optimize;", cancellationToken);
    }

    /// <summary>
    /// Asynchronously executes <c>PRAGMA optimize</c>.
    /// </summary>
    public virtual Task OptimizeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Optimize(cancellationToken);
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

    public new virtual SqliteTransaction BeginTransaction()
        => BeginTransaction(IsolationLevel.Unspecified);

    public new virtual SqliteTransaction BeginTransaction(IsolationLevel isolationLevel)
    {
        lock (_syncRoot)
        {
            EnsureOpen(nameof(BeginTransaction));
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

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        => BeginTransaction(isolationLevel);

    public new virtual SqliteCommand CreateCommand()
    {
        return new SqliteCommand
        {
            Connection = this,
            Transaction = _activeTransaction,
        };
    }

    protected override DbCommand CreateDbCommand()
        => CreateCommand();

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


    internal bool HasActiveTransaction()
    {
        lock (_syncRoot)
        {
            return _activeTransaction is not null;
        }
    }

    internal IDisposable AcquireWriterGate(CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            EnsureOpen();
            return SingleWriterCoordinator.Acquire(_writerKey, cancellationToken);
        }
    }

    private static string ResolveWriterKey(string connectionString, string dataSource)
    {
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return $"cs:{connectionString}";
        }

        if (!string.IsNullOrWhiteSpace(dataSource))
        {
            return $"ds:{dataSource}";
        }

        return "memory";
    }

    private void ExecuteSessionPragma(string sql, CancellationToken cancellationToken)
    {
        SqliteSession session = GetSession();
        session.Gate.Wait(cancellationToken);
        try
        {
            session.Native.Execute(sql);
        }
        catch (SqliteInteropException ex)
        {
            throw new SqliteException(ex.Message, ex);
        }
        finally
        {
            session.Gate.Release();
        }
    }

    private static string ToCheckpointPragma(SqliteWalCheckpointMode mode)
        => mode switch
        {
            SqliteWalCheckpointMode.Passive => "PASSIVE",
            SqliteWalCheckpointMode.Full => "FULL",
            SqliteWalCheckpointMode.Restart => "RESTART",
            SqliteWalCheckpointMode.Truncate => "TRUNCATE",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported WAL checkpoint mode."),
        };


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
        native.SetExtendedResultCodes(true);
        native.SetBusyTimeout(Math.Max(0, _settings.DefaultTimeout * 1000));

        // Intelligent default: Foreign Keys ON if not specified
        bool foreignKeysSpecified = _settings.HasForeignKeys;
        bool foreignKeysValue = foreignKeysSpecified ? (_settings.ForeignKeys ?? false) : true;
        native.Execute($"PRAGMA foreign_keys={(foreignKeysValue ? "ON" : "OFF")};");

        // Intelligent default: Journal Mode WAL if not specified, except for in-memory
        bool isInMemory = _settings.IsInMemoryMode();
        if (!_settings.HasJournalMode && !isInMemory)
        {
            native.Execute("PRAGMA journal_mode=WAL;");
        }
        else if (_settings.HasJournalMode && !string.IsNullOrWhiteSpace(_settings.JournalMode))
        {
            native.Execute($"PRAGMA journal_mode={_settings.JournalMode};");
        }
        else if (isInMemory)
        {
            // In-memory: WAL not supported, force DELETE
            native.Execute("PRAGMA journal_mode=DELETE;");
        }

        // Recursive triggers if specified
        if (_settings.RecursiveTriggers.HasValue)
        {
            native.Execute($"PRAGMA recursive_triggers={(_settings.RecursiveTriggers.Value ? "ON" : "OFF")};");
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

        // Intelligent default for Mode=Memory: if user specified Mode=Memory but not Cache=Shared,
        // we force Shared cache.
        if (_settings.IsInMemoryMode() && !string.IsNullOrEmpty(dataSource) && dataSource != ":memory:")
        {
            string cacheSuffix = string.Empty;
            // if (string.IsNullOrEmpty(_settings.Cache) ||
            //     string.Equals(_settings.Cache, "Shared", StringComparison.OrdinalIgnoreCase))
            // {
                cacheSuffix = "&cache=shared";
            // }
            // else if (string.Equals(_settings.Cache, "Private", StringComparison.OrdinalIgnoreCase))
            // {
            //     cacheSuffix = "&cache=private";
            // }
            return $"file:{dataSource}?mode=memory{cacheSuffix}";
        }

        return Path.IsPathRooted(dataSource)
            ? dataSource
            : Path.Combine(AppContext.BaseDirectory, dataSource);
    }

    public virtual void BackupDatabase(SqliteConnection destination)
    {
        throw new NotImplementedException("Not Implemented");
    }

     public virtual void BackupDatabase(SqliteConnection destination, string destinationName, string sourceName)
     {
        throw new NotImplementedException("Not Implemented");
     }

    /// <summary>
    ///     Gets or sets the transaction currently being used by the connection, or null if none.
    /// </summary>
    /// <value>The transaction currently being used by the connection.</value>
    protected internal virtual SqliteTransaction? Transaction
    {
        get
        {
            if (_activeTransaction != null && GetSession().Native.IsAutoCommit())
            {
                ClearActiveTransaction();
            }

            return _activeTransaction;
        }
        set => _activeTransaction = value;
    }
}
