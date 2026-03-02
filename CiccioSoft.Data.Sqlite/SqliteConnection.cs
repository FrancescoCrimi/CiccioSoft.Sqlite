using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using CiccioSoft.Sqlite.Interop;
using static CiccioSoft.Sqlite.Interop.Native.sqlite3;

namespace CiccioSoft.Data.Sqlite;

public class SqliteConnection : DbConnection
{
    private const int DefaultBusyTimeoutMs = 30000;
    private readonly object _syncRoot = new();
    private string _connectionString = string.Empty;
    private ConnectionState _state = ConnectionState.Closed;
    private SqliteSession? _session;
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

    public SqliteConnectionProfile Profile => _settings.Profile;

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

                ApplyProfileSettings(session.Native);

                _session = session;
                _state = ConnectionState.Open;
            }
            catch (SqliteInteropException ex)
            {
                throw new SqliteException(ex.Message, ex.BaseErrorCode, ex.ExtendedErrorCode, ex);
            }
        }
    }

    public override async Task OpenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Run(Open, cancellationToken).ConfigureAwait(false);
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        EnsureOpen();
        return new SqliteTransaction(this, isolationLevel);
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

    private bool IsPoolingEnabled()
    {
        return _settings.Profile == SqliteConnectionProfile.Default && _settings.Pooling;
    }

    private int GetOpenFlags()
    {
        int flags = SQLITE_OPEN_READWRITE | SQLITE_OPEN_CREATE;
        if (_settings.Profile == SqliteConnectionProfile.Default)
        {
            flags |= SQLITE_OPEN_FULLMUTEX;
        }

        return flags;
    }

    private void ApplyProfileSettings(Sqlite3 native)
    {
        if (_settings.Profile == SqliteConnectionProfile.StrictSingleConnection)
        {
            native.SetBusyTimeout(0);
            native.Execute("PRAGMA foreign_keys=ON;");
            return;
        }

        native.SetBusyTimeout(Math.Max(DefaultBusyTimeoutMs, _settings.BusyTimeout));
        native.Execute("PRAGMA foreign_keys=ON;");
        native.Execute("PRAGMA journal_mode=WAL;");
    }
}
