// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using CiccioSoft.Data.Sqlite.Properties;
using CiccioSoft.Sqlite.Interop;
using static CiccioSoft.Sqlite.Interop.Native.sqlite3;

namespace CiccioSoft.Data.Sqlite;

public class SqliteCommand : DbCommand
{
    private readonly SqliteParameterCollection _parameters = new();
    private SqliteConnection? _connection;

    public SqliteCommand() { }

    public SqliteCommand(string commandText, SqliteConnection connection)
    {
        CommandText = commandText;
        Connection = connection;
    }

    private string _commandText = string.Empty;

    public override string CommandText
    {
        get => _commandText;
        set => _commandText = value ?? string.Empty;
    }

    private int _commandTimeout = 30;
    public override int CommandTimeout
    {
        get => _commandTimeout;
        set => _commandTimeout = value < 0
            ? throw new ArgumentOutOfRangeException(nameof(value), value, "CommandTimeout cannot be negative.")
            : value;
    }

    private CommandType _commandType = CommandType.Text;

    public override CommandType CommandType
    {
        get => _commandType;
        set
        {
            if (value != CommandType.Text)
            {
                throw new NotSupportedException(Properties.Resources.InvalidCommandType(value));
            }

            _commandType = value;
        }
    }

    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }

    public new SqliteConnection? Connection
    {
        get => _connection;
        set => _connection = value;
    }

    protected override DbConnection? DbConnection
    {
        get => _connection;
        set => _connection = (SqliteConnection?)value;
    }

    protected override DbParameterCollection DbParameterCollection => _parameters;

    public new SqliteTransaction? Transaction { get; set; }

    protected override DbTransaction? DbTransaction
    {
        get => Transaction;
        set => Transaction = (SqliteTransaction?)value;
    }

    public override void Cancel()
    {
        if (_connection is null || _connection.State != ConnectionState.Open)
            return;

        _connection.GetSession().Native.Interrupt();
    }

    public override int ExecuteNonQuery()
    {
        SqliteConnection conn = RequireOpenConnection(nameof(ExecuteNonQuery));
        ValidateTransaction(conn);
        SqliteSession session = conn.GetSession();
        session.Gate.Wait();
        try
        {
            using CommandExecutionScope scope = CreateExecutionScope(session, CancellationToken.None);
            BatchExecutionState batchState = new(CommandText);
            while (true)
            {
                using Sqlite3Stmt? stmt = scope.Execute(() => PrepareAndBindNext(session, batchState));
                if (stmt is null)
                {
                    break;
                }

                while (scope.Execute(stmt.Step)) { }
            }

            return session.Native.Changes();
        }
        finally
        {
            session.Gate.Release();
        }
    }

    public override object? ExecuteScalar()
    {
        using DbDataReader reader = ExecuteReader(CommandBehavior.SingleRow);
        return reader.Read() ? reader.GetValue(0) : null;
    }

    public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        SqliteConnection conn = RequireOpenConnection(nameof(ExecuteNonQuery));
        ValidateTransaction(conn);
        SqliteSession session = conn.GetSession();
        await session.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using CommandExecutionScope scope = CreateExecutionScope(session, cancellationToken);
            BatchExecutionState batchState = new(CommandText);
            while (true)
            {
                using Sqlite3Stmt? stmt = scope.Execute(() => PrepareAndBindNext(session, batchState));
                if (stmt is null)
                {
                    break;
                }

                while (scope.Execute(stmt.Step)) { }
            }

            return session.Native.Changes();
        }
        finally
        {
            session.Gate.Release();
        }
    }

    public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
    {
        await using DbDataReader reader = await ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? reader.GetValue(0) : null;
    }

    public override void Prepare()
    {
        SqliteConnection conn = RequireOpenConnection(nameof(Prepare));
        ValidateTransaction(conn);
        SqliteSession session = conn.GetSession();
        session.Gate.Wait();
        try
        {
            using CommandExecutionScope scope = CreateExecutionScope(session, CancellationToken.None);
            using Sqlite3Stmt stmt = scope.Execute(() => session.Native.Prepare(CommandText));
        }
        finally
        {
            session.Gate.Release();
        }
    }

    public override async Task PrepareAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        SqliteConnection conn = RequireOpenConnection(nameof(Prepare));
        ValidateTransaction(conn);
        SqliteSession session = conn.GetSession();
        await session.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using CommandExecutionScope scope = CreateExecutionScope(session, cancellationToken);
            using Sqlite3Stmt stmt = scope.Execute(() => session.Native.Prepare(CommandText));
        }
        finally
        {
            session.Gate.Release();
        }
    }

    internal CommandExecutionScope CreateExecutionScope(SqliteSession session, CancellationToken cancellationToken)
        => new(this, session, cancellationToken);

    internal sealed class CommandExecutionScope : IDisposable
    {
        private readonly SqliteCommand _command;
        private readonly CancellationToken _externalCancellationToken;
        private readonly SqliteSession _session;
        private readonly CancellationTokenSource _timeoutSource;
        private readonly CancellationTokenSource _linkedSource;
        private readonly CancellationTokenRegistration _timeoutRegistration;
        private readonly CancellationTokenRegistration _interruptRegistration;
        private bool _timeoutTriggered;

        public CommandExecutionScope(SqliteCommand command, SqliteSession session, CancellationToken cancellationToken)
        {
            _command = command;
            _session = session;
            _externalCancellationToken = cancellationToken;
            _timeoutSource = command.CreateTimeoutSource();
            _linkedSource = _timeoutSource.Token.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _timeoutSource.Token)
                : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _timeoutRegistration = _timeoutSource.Token.Register(() => _timeoutTriggered = true);
            _interruptRegistration = _linkedSource.Token.Register(() => session.Native.Interrupt());
        }

        public T Execute<T>(Func<T> operation, CancellationToken operationCancellationToken = default)
        {
            bool operationCanceled = false;
            using CancellationTokenRegistration operationRegistration = operationCancellationToken.CanBeCanceled
                ? operationCancellationToken.Register(() => operationCanceled = true)
                : default;
            using CancellationTokenRegistration operationInterruptRegistration = operationCancellationToken.CanBeCanceled
                ? operationCancellationToken.Register(() => _session.Native.Interrupt())
                : default;

            try
            {
                _externalCancellationToken.ThrowIfCancellationRequested();
                operationCancellationToken.ThrowIfCancellationRequested();
                return operation();
            }
            catch (SqliteInteropException ex) when (_timeoutTriggered && ex.BaseErrorCode == SQLITE_INTERRUPT)
            {
                throw new SqliteException(Properties.Resources.CommandTimedOut(_command.CommandTimeout), SQLITE_INTERRUPT, ex.ExtendedErrorCode, ex);
            }
            catch (SqliteInteropException ex) when ((operationCanceled || operationCancellationToken.IsCancellationRequested) && ex.BaseErrorCode == SQLITE_INTERRUPT)
            {
                throw new OperationCanceledException(operationCancellationToken);
            }
            catch (SqliteInteropException ex) when (_externalCancellationToken.IsCancellationRequested && ex.BaseErrorCode == SQLITE_INTERRUPT)
            {
                throw new OperationCanceledException(_externalCancellationToken);
            }
            catch (SqliteInteropException ex)
            {
                throw new SqliteException(ex.Message, ex.BaseErrorCode, ex.ExtendedErrorCode, ex);
            }
        }

        public void Dispose()
        {
            _interruptRegistration.Dispose();
            _timeoutRegistration.Dispose();
            _linkedSource.Dispose();
            _timeoutSource.Dispose();
        }
    }

    private CancellationTokenSource CreateTimeoutSource()
    {
        if (CommandTimeout <= 0)
        {
            return new CancellationTokenSource();
        }

        return new CancellationTokenSource(TimeSpan.FromSeconds(CommandTimeout));
    }

    protected override DbParameter CreateDbParameter() => new SqliteParameter();

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        SqliteConnection conn = RequireOpenConnection(nameof(ExecuteReader));
        ValidateTransaction(conn);
        return SqliteDataReader.Create(this, conn.GetSession(), behavior);
    }

    protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SqliteConnection conn = RequireOpenConnection(nameof(ExecuteReader));
        ValidateTransaction(conn);
        return await SqliteDataReader.CreateAsync(this, conn.GetSession(), behavior, cancellationToken).ConfigureAwait(false);
    }

    internal sealed class BatchExecutionState
    {
        private int _sqlByteOffset;

        public BatchExecutionState(string sql)
        {
            Sql = sql;
        }

        public string Sql { get; }

        public int SqlByteOffset
        {
            get => _sqlByteOffset;
            set => _sqlByteOffset = value;
        }
    }

    internal Sqlite3Stmt PrepareAndBind(SqliteSession session)
    {
        Sqlite3Stmt stmt = session.Native.Prepare(CommandText);
        BindParameters(stmt, throwOnMissingParameter: true);
        return stmt;
    }

    internal Sqlite3Stmt? PrepareAndBindNext(SqliteSession session, BatchExecutionState batchState)
    {
        Sqlite3Stmt? stmt = session.Native.Prepare(batchState.Sql, batchState.SqlByteOffset, out int nextSqlByteOffset);
        batchState.SqlByteOffset = nextSqlByteOffset;
        if (stmt is null)
        {
            return null;
        }

        BindParameters(stmt, throwOnMissingParameter: false);
        return stmt;
    }

    private void BindParameters(Sqlite3Stmt stmt, bool throwOnMissingParameter)
    {
        for (int i = 0; i < _parameters.Count; i++)
        {
            SqliteParameter parameter = (SqliteParameter)_parameters[i]!;
            ValidateParameterDirection(parameter);
            int parameterIndex = ResolveParameterIndex(stmt, parameter, i, throwOnMissingParameter);
            if (parameterIndex == 0)
            {
                continue;
            }

            BindParameter(stmt, parameterIndex, parameter);
        }
    }


    private static void ValidateParameterDirection(SqliteParameter parameter)
    {
        if (parameter.Direction != ParameterDirection.Input)
        {
            throw new NotSupportedException(Resources.InvalidParameterDirection(parameter.Direction));
        }
    }

    private static int ResolveParameterIndex(Sqlite3Stmt stmt, SqliteParameter parameter, int ordinal, bool throwOnMissingParameter)
    {
        string parameterName = parameter.ParameterName;
        if (string.IsNullOrWhiteSpace(parameterName))
        {
            return ordinal + 1;
        }

        int index = stmt.GetParameterIndex(parameterName);
        if (index > 0)
        {
            return index;
        }

        string coreName = parameterName[0] is ('@' or ':' or '$')
            ? parameterName.Substring(1)
            : parameterName;

        index = stmt.GetParameterIndex($"@{coreName}");
        if (index > 0)
        {
            return index;
        }

        index = stmt.GetParameterIndex($":{coreName}");
        if (index > 0)
        {
            return index;
        }

        index = stmt.GetParameterIndex($"${coreName}");
        if (index > 0)
        {
            return index;
        }

        if (throwOnMissingParameter)
        {
            throw new InvalidOperationException($"Parameter '{parameterName}' does not exist in the command text.");
        }

        return 0;
    }

    private static void BindParameter(Sqlite3Stmt stmt, int index, SqliteParameter parameter)
    {
        object? value = parameter.Value;
        if (value is null || value is DBNull)
        {
            stmt.BindNull(index);
            return;
        }

        switch (value)
        {
            case int i: stmt.BindInt(index, i); break;
            case long l: stmt.BindLong(index, l); break;
            case short s: stmt.BindInt(index, s); break;
            case sbyte sb: stmt.BindInt(index, sb); break;
            case byte b: stmt.BindInt(index, b); break;
            case uint ui: stmt.BindLong(index, ui); break;
            case ulong ul when ul <= long.MaxValue: stmt.BindLong(index, (long)ul); break;
            case ulong ul: stmt.BindText(index, ul.ToString(System.Globalization.CultureInfo.InvariantCulture)); break;
            case ushort us: stmt.BindInt(index, us); break;
            case bool bo: stmt.BindInt(index, bo ? 1 : 0); break;
            case float f: stmt.BindDouble(index, f); break;
            case double d: stmt.BindDouble(index, d); break;
            case decimal m: stmt.BindText(index, m.ToString(System.Globalization.CultureInfo.InvariantCulture)); break;
            case char c: stmt.BindText(index, c.ToString()); break;
            case Guid guid: stmt.BindText(index, guid.ToString("D").ToUpperInvariant()); break;
            case DateTime dateTime: stmt.BindText(index, dateTime.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFF", System.Globalization.CultureInfo.InvariantCulture)); break;
            case DateTimeOffset dateTimeOffset: stmt.BindText(index, dateTimeOffset.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFFzzz", System.Globalization.CultureInfo.InvariantCulture)); break;
            case TimeSpan timeSpan: stmt.BindText(index, timeSpan.ToString("c", System.Globalization.CultureInfo.InvariantCulture)); break;
            case Enum enumValue: stmt.BindLong(index, Convert.ToInt64(enumValue, System.Globalization.CultureInfo.InvariantCulture)); break;
            case byte[] bytes: stmt.BindBlob(index, bytes); break;
            default: stmt.BindText(index, Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty); break;
        }
    }

    private SqliteConnection RequireOpenConnection(string methodName)
    {
        if (_connection is null || _connection.State != ConnectionState.Open)
        {
            throw new InvalidOperationException(Resources.CallRequiresOpenConnection(methodName));
        }

        return _connection;
    }

    private void ValidateTransaction(SqliteConnection connection)
    {
        if (Transaction is null)
        {
            return;
        }

        if (Transaction.Connection is null)
        {
            throw new InvalidOperationException(Resources.TransactionCompleted);
        }

        if (!ReferenceEquals(Transaction.Connection, connection))
        {
            throw new InvalidOperationException(Resources.TransactionConnectionMismatch);
        }
    }
}
