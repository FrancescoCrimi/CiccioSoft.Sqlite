// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
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

    /// <summary>
    /// Checks if the underlying connection is still valid.
    /// </summary>
    public bool IsValid()
    {
        try
        {
            // Execute a lightweight query to test the connection
            Native.Execute("SELECT 1");
            return true;
        }
        catch
        {
            return false;
        }
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

    public static async Task<IDisposable> AcquireAsync(string writerKey, CancellationToken cancellationToken)
    {
        SemaphoreSlim gate = Gates.GetOrAdd(writerKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new Releaser(gate);
    }
}

internal static class SqliteConnectionPool
{
    private sealed class PoolState
    {
        public readonly ConcurrentBag<SqliteSession> Bag = new();
        public readonly SemaphoreSlim Semaphore = new(0, int.MaxValue);
        public int Count;
    }

    private static readonly ConcurrentDictionary<string, PoolState> Pools = new(StringComparer.Ordinal);

    /// <summary>
    /// Rents a connection from the pool. Creates a new one if available and under max pool size.
    /// </summary>
    public static SqliteSession Rent(string connectionString, string dataSource, int maxPoolSize, SqliteOpenFlags openFlags)
    {
        PoolState state = Pools.GetOrAdd(connectionString, _ => new PoolState());

        // Try to get an existing session from the bag
        if (state.Bag.TryTake(out SqliteSession? session))
        {
            // Validate the session before returning
            if (session.IsValid())
            {
                return session;
            }

            // Session is dead, dispose it and decrement count
            session.Dispose();
            Interlocked.Decrement(ref state.Count);
        }

        // Create a new session, respecting maxPoolSize
        while (true)
        {
            int current = Volatile.Read(ref state.Count);
            if (current >= maxPoolSize)
            {
                // Pool is full, wait for a session to be returned
                state.Semaphore.Wait();
                if (state.Bag.TryTake(out session))
                {
                    if (session.IsValid())
                    {
                        return session;
                    }

                    // Dead session, dispose and continue waiting
                    session.Dispose();
                    Interlocked.Decrement(ref state.Count);
                    continue;
                }
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
    
    /// <summary>
    /// Asynchronously rents a connection from the pool.
    /// </summary>
    public static async Task<SqliteSession> RentAsync(
        string connectionString,
        string dataSource,
        int maxPoolSize,
        SqliteOpenFlags openFlags,
        CancellationToken cancellationToken = default)
    {
        PoolState state = Pools.GetOrAdd(connectionString, _ => new PoolState());

        // Try to get an existing session from the bag
        if (state.Bag.TryTake(out SqliteSession? session))
        {
            if (session.IsValid())
            {
                return session;
            }

            session.Dispose();
            Interlocked.Decrement(ref state.Count);
        }

        // Create a new session, respecting maxPoolSize
        while (true)
        {
            int current = Volatile.Read(ref state.Count);
            if (current >= maxPoolSize)
            {
                // Pool is full, wait for a session to be returned
                await state.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                if (state.Bag.TryTake(out session))
                {
                    if (session.IsValid())
                    {
                        return session;
                    }

                    session.Dispose();
                    Interlocked.Decrement(ref state.Count);
                    continue;
                }
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

    /// <summary>
    /// Returns a connection to the pool.
    /// </summary>
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

    /// <summary>
    /// Clears all connections in the pool for the given connection string.
    /// </summary>
    public static void Clear(string connectionString)
    {
        if (Pools.TryRemove(connectionString, out PoolState? state))
        {
            while (state.Bag.TryTake(out SqliteSession? session))
            {
                session.Dispose();
            }
            state.Semaphore.Dispose();
        }
    }
}
