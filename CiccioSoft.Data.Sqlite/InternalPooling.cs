// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections.Concurrent;
using System.Threading;
using CiccioSoft.Sqlite.Interop;

namespace CiccioSoft.Data.Sqlite;

internal sealed class SqliteSession : IDisposable
{
    public Sqlite3 Native { get; }
    public SemaphoreSlim Gate { get; } = new(1, 1);

    public SqliteSession(Sqlite3 native)
    {
        Native = native;
    }

    public void Dispose()
    {
        Gate.Dispose();
        Native.Dispose();
    }
}

internal static class SingleWriterCoordinator
{
    private sealed class Releaser : IDisposable
    {
        private readonly SemaphoreSlim _gate;
        private bool _disposed;

        public Releaser(SemaphoreSlim gate)
        {
            _gate = gate;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _gate.Release();
        }
    }

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new(StringComparer.OrdinalIgnoreCase);

    public static IDisposable Acquire(string writerKey, CancellationToken cancellationToken)
    {
        SemaphoreSlim gate = Gates.GetOrAdd(writerKey, _ => new SemaphoreSlim(1, 1));
        gate.Wait(cancellationToken);
        return new Releaser(gate);
    }
}

internal static class SqliteConnectionPool
{
    private sealed class PoolState
    {
        public readonly ConcurrentBag<SqliteSession> Bag = new();
        public int Count;
    }

    private static readonly ConcurrentDictionary<string, PoolState> Pools = new(StringComparer.Ordinal);

    public static SqliteSession Rent(string connectionString, string dataSource, int maxPoolSize, SqliteOpenFlags openFlags)
    {
        PoolState state = Pools.GetOrAdd(connectionString, _ => new PoolState());
        if (state.Bag.TryTake(out SqliteSession? session))
        {
            return session;
        }

        while (true)
        {
            int current = Volatile.Read(ref state.Count);
            if (current >= maxPoolSize)
            {
                SpinWait.SpinUntil(() => state.Bag.TryTake(out session), 100);
                if (session is not null)
                    return session;
                continue;
            }

            if (Interlocked.CompareExchange(ref state.Count, current + 1, current) == current)
            {
                try
                {
                    return new SqliteSession(Sqlite3.Open(dataSource, openFlags));
                }
                catch
                {
                    Interlocked.Decrement(ref state.Count);
                    throw;
                }
            }
        }
    }

    public static void Return(string connectionString, SqliteSession session)
    {
        if (Pools.TryGetValue(connectionString, out PoolState? state))
        {
            state.Bag.Add(session);
        }
        else
        {
            session.Dispose();
        }
    }
}
