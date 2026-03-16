// Copyright (c) 2026 Francesco Crimi
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CiccioSoft.Data.Sqlite;
using CiccioSoft.Data.Sqlite.Properties;
using Xunit;

namespace CiccioSoft.Data.Sqlite.Tests;

public class SqliteTransactionSemanticsTests
{
    [Fact]
    public void BeginTransaction_DefaultsToSerializable()
    {
        using SqliteConnection connection = new("Data Source=:memory:;");
        connection.Open();

        using SqliteTransaction transaction = (SqliteTransaction)connection.BeginTransaction();

        Assert.Equal(IsolationLevel.Serializable, transaction.IsolationLevel);
    }

    [Theory]
    [InlineData(IsolationLevel.ReadCommitted)]
    [InlineData(IsolationLevel.RepeatableRead)]
    [InlineData(IsolationLevel.Unspecified)]
    public void BeginTransaction_NormalizesIsolationLevel(IsolationLevel requested)
    {
        using SqliteConnection connection = new("Data Source=:memory:;");
        connection.Open();

        using SqliteTransaction transaction = (SqliteTransaction)connection.BeginTransaction(requested);

        Assert.Equal(IsolationLevel.Serializable, transaction.IsolationLevel);
    }

    [Fact]
    public void BeginTransaction_KeepsReadUncommitted()
    {
        using SqliteConnection connection = new("Data Source=:memory:;");
        connection.Open();

        using SqliteTransaction transaction = (SqliteTransaction)connection.BeginTransaction(IsolationLevel.ReadUncommitted);

        Assert.Equal(IsolationLevel.ReadUncommitted, transaction.IsolationLevel);
    }

    [Theory]
    [InlineData(IsolationLevel.Chaos)]
    [InlineData(IsolationLevel.Snapshot)]
    public void BeginTransaction_ThrowsForUnsupportedLevels(IsolationLevel isolationLevel)
    {
        using SqliteConnection connection = new("Data Source=:memory:;");
        connection.Open();

        ArgumentException ex = Assert.Throws<ArgumentException>(() => connection.BeginTransaction(isolationLevel));

        Assert.StartsWith(Resources.InvalidIsolationLevel(isolationLevel), ex.Message, StringComparison.Ordinal);
        Assert.Equal("isolationLevel", ex.ParamName);
    }

    [Fact]
    public void Commit_ThrowsTransactionCompletedAfterDispose()
    {
        using SqliteConnection connection = new("Data Source=:memory:;");
        connection.Open();

        SqliteTransaction transaction = (SqliteTransaction)connection.BeginTransaction();
        transaction.Dispose();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => transaction.Commit());

        Assert.Equal(Resources.TransactionCompleted, ex.Message);
    }

    [Fact]
    public void Rollback_ThrowsTransactionCompletedAfterCommit()
    {
        using SqliteConnection connection = new("Data Source=:memory:;");
        connection.Open();

        SqliteTransaction transaction = (SqliteTransaction)connection.BeginTransaction();
        transaction.Commit();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => transaction.Rollback());

        Assert.Equal(Resources.TransactionCompleted, ex.Message);
    }

    [Fact]
    public async Task SingleWriterPattern_SerializesConcurrentWritersWithinProcess()
    {
        string dbPath = Path.Combine(AppContext.BaseDirectory, $"single-writer-{Guid.NewGuid():N}.db");
        string cs = $"Data Source={dbPath};BusyTimeout=0;";

        try
        {
            using SqliteConnection setupConnection = new(cs);
            setupConnection.Open();
            setupConnection.ExecuteNonQuery("PRAGMA journal_mode=WAL; CREATE TABLE t(id INTEGER PRIMARY KEY, value TEXT);");

            using SqliteConnection writer1 = new(cs);
            using SqliteConnection writer2 = new(cs);
            writer1.Open();
            writer2.Open();

            using SqliteTransaction tx = (SqliteTransaction)writer1.BeginTransaction();
            writer1.ExecuteNonQuery("INSERT INTO t(value) VALUES ('first');");

            Stopwatch stopwatch = Stopwatch.StartNew();
            Task secondWrite = Task.Run(() => writer2.ExecuteNonQuery("INSERT INTO t(value) VALUES ('second');"));

            await Task.Delay(200);
            tx.Commit();

            await secondWrite;
            stopwatch.Stop();

            Assert.True(stopwatch.ElapsedMilliseconds >= 150);
            Assert.Equal(2L, writer1.ExecuteScalar<long>("SELECT COUNT(*) FROM t;"));
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }

            string walPath = dbPath + "-wal";
            if (File.Exists(walPath))
            {
                File.Delete(walPath);
            }

            string shmPath = dbPath + "-shm";
            if (File.Exists(shmPath))
            {
                File.Delete(shmPath);
            }
        }
    }
}
