// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;

namespace CiccioSoft.Sqlite.Native;

/// <summary>
/// Represents a logical SQLite root transaction owned by a <see cref="Connection"/>.
/// </summary>
public sealed class Transaction : IDisposable
{
    private readonly Connection _connection;
    private readonly object _syncRoot = new();
    private LogicalTransactionState _state;

    #region ctor

    internal Transaction(Connection connection, TransactionMode mode)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        Mode = mode;
        _state = LogicalTransactionState.Initial;
    }

    #endregion


    #region public

    public void Commit()
    {
        lock (_syncRoot)
        {
            EnsureState(LogicalTransactionState.Active, nameof(Commit));
            _state = LogicalTransactionState.Committing;
        }

        try
        {
            _connection.Execute("COMMIT;");
            Complete();
        }
        catch
        {
            Fail();
            throw;
        }
    }

    public void Rollback()
    {
        lock (_syncRoot)
        {
            EnsureState(LogicalTransactionState.Active, nameof(Rollback));
            _state = LogicalTransactionState.RollingBack;
        }

        try
        {
            _connection.Execute("ROLLBACK;");
            Complete();
        }
        catch
        {
            Fail();
            throw;
        }
    }

    public TransactionMode Mode { get; }

    public LogicalTransactionState State
    {
        get
        {
            lock (_syncRoot)
            {
                return _state;
            }
        }
    }

    #endregion


    #region internal

    internal void Activate()
    {
        try
        {
            _connection.Execute(GetBeginSql(Mode));
            lock (_syncRoot)
            {
                EnsureState(LogicalTransactionState.Initial, nameof(Activate));
                _state = LogicalTransactionState.Active;
            }
        }
        catch
        {
            MarkFailed();
            Complete();
            throw;
        }
    }

    internal bool IsRegisteredActive
    {
        get
        {
            lock (_syncRoot)
            {
                return _state is LogicalTransactionState.Initial or LogicalTransactionState.Active or LogicalTransactionState.Committing or LogicalTransactionState.RollingBack;
            }
        }
    }

    internal void MarkFailed()
    {
        Fail();
    }

    #endregion


    #region private

    private static string GetBeginSql(TransactionMode mode)
    {
        return mode switch
        {
            TransactionMode.Deferred => "BEGIN DEFERRED;",
            TransactionMode.Immediate => "BEGIN IMMEDIATE;",
            TransactionMode.Exclusive => "BEGIN EXCLUSIVE;",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "The transaction mode is not supported.")
        };
    }

    private void Complete()
    {
        lock (_syncRoot)
        {
            _state = LogicalTransactionState.Completed;
        }

        _connection.ClearRootTransaction(this);
    }

    private void Fail()
    {
        lock (_syncRoot)
        {
            _state = LogicalTransactionState.Failed;
        }
    }

    private void EnsureState(LogicalTransactionState expected, string operation)
    {
        if (_state != expected)
        {
            throw new InvalidOperationException($"Transaction.{operation} is invalid while the transaction is in the {_state} state.");
        }
    }

    #endregion


    #region disposable

    public void Dispose()
    {
        if (State == LogicalTransactionState.Active)
        {
            Rollback();
        }
    }

    #endregion
}
