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

namespace CiccioSoft.Data.Sqlite;

public class SqliteTransaction : DbTransaction
{
    private readonly SqliteConnection _connection;
    private bool _completed;

    internal SqliteTransaction(SqliteConnection connection, IsolationLevel isolationLevel)
    {
        _connection = connection;
        IsolationLevel = NormalizeIsolationLevel(isolationLevel);
        Execute(GetBeginStatement(IsolationLevel));
    }

    public override IsolationLevel IsolationLevel { get; }

    protected override DbConnection? DbConnection => _completed ? null : _connection;

    public override void Commit()
    {
        EnsureActive();
        Execute("COMMIT;");
        _completed = true;
        _connection.ClearActiveTransaction();
    }

    public override void Rollback()
    {
        EnsureActive();
        Execute("ROLLBACK;");
        _completed = true;
        _connection.ClearActiveTransaction();
    }

    public override Task CommitAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Commit();
        return Task.CompletedTask;
    }

    public override Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Rollback();
        return Task.CompletedTask;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_completed && _connection.State == ConnectionState.Open)
        {
            try { Rollback(); } catch { }
        }

        if (disposing)
        {
            _connection.ClearActiveTransaction();
        }

        base.Dispose(disposing);
    }

    private void EnsureActive()
    {
        if (_completed || _connection.State != ConnectionState.Open)
        {
            throw new InvalidOperationException(Resources.TransactionCompleted);
        }

        _connection.EnsureOpen();
    }

    private void Execute(string sql)
    {
        SqliteSession session = _connection.GetSession();
        session.Gate.Wait();
        try
        {
            session.Native.Execute(sql);
        }
        catch (SqliteInteropException ex)
        {
            throw new SqliteException(ex.Message, ex.BaseErrorCode, ex.ExtendedErrorCode, ex);
        }
        finally
        {
            session.Gate.Release();
        }
    }

    private static IsolationLevel NormalizeIsolationLevel(IsolationLevel isolationLevel)
    {
        return isolationLevel switch
        {
            IsolationLevel.Unspecified => IsolationLevel.Serializable,
            IsolationLevel.Chaos => throw new ArgumentException(Resources.InvalidIsolationLevel(isolationLevel), nameof(isolationLevel)),
            IsolationLevel.Snapshot => throw new ArgumentException(Resources.InvalidIsolationLevel(isolationLevel), nameof(isolationLevel)),
            IsolationLevel.ReadCommitted => IsolationLevel.Serializable,
            IsolationLevel.RepeatableRead => IsolationLevel.Serializable,
            IsolationLevel.ReadUncommitted => IsolationLevel.ReadUncommitted,
            _ => isolationLevel,
        };
    }

    private static string GetBeginStatement(IsolationLevel isolationLevel)
    {
        return isolationLevel == IsolationLevel.ReadUncommitted
            ? "PRAGMA read_uncommitted=1; BEGIN;"
            : "PRAGMA read_uncommitted=0; BEGIN IMMEDIATE;";
    }
}
