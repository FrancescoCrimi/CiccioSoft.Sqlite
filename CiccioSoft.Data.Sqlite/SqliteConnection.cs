using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using CiccioSoft.Sqlite.Interop;

namespace CiccioSoft.Data.Sqlite;

public class SqliteConnection : DbConnection
{
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
        lock (_syncRoot)
        {
            if (_state == ConnectionState.Closed)
                return;

            session = Interlocked.Exchange(ref _session, null);
            _state = ConnectionState.Closed;
        }

        if (session is null)
            return;

        if (_settings.Pooling)
            SqliteConnectionPool.Return(_connectionString, session);
        else
            session.Dispose();
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
                SqliteSession session = _settings.Pooling
                    ? SqliteConnectionPool.Rent(_connectionString, _settings.DataSource, _settings.MaxPoolSize)
                    : new SqliteSession(Sqlite3.Open(_settings.DataSource));

                session.Native.SetBusyTimeout(_settings.BusyTimeout);
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
}
