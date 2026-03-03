// Copyright (c) 2026 Francesco Crimi
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using CiccioSoft.Data.Sqlite;
using Xunit;

namespace CiccioSoft.Data.Sqlite.Tests;

public class SqliteBehaviorTests
{
    [Fact]
    public void HasRows_ReflectsWhetherRowsExist()
    {
        using SqliteConnection connection = new("Data Source=:memory:;");
        connection.Open();

        using (SqliteCommand setup = new("CREATE TABLE t(id INTEGER);", connection))
        {
            setup.ExecuteNonQuery();
        }

        using SqliteCommand query = new("SELECT id FROM t;", connection);
        using SqliteDataReader reader = (SqliteDataReader)query.ExecuteReader();

        Assert.False(reader.HasRows);
    }

    [Fact]
    public void GetOrdinal_ThrowsForUnknownColumn()
    {
        using SqliteConnection connection = new("Data Source=:memory:;");
        connection.Open();

        using SqliteCommand query = new("SELECT 1 AS known;", connection);
        using SqliteDataReader reader = (SqliteDataReader)query.ExecuteReader();
        Assert.True(reader.Read());

        Assert.Throws<IndexOutOfRangeException>(() => reader.GetOrdinal("missing"));
    }

    [Fact]
    public async Task ExecuteReaderAsync_WithPreCanceledToken_Throws()
    {
        using SqliteConnection connection = new("Data Source=:memory:;");
        connection.Open();

        using SqliteCommand query = new("SELECT 1;", connection);
        using CancellationTokenSource cts = new();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => query.ExecuteReaderAsync(CommandBehavior.Default, cts.Token));
    }

    [Fact]
    public async Task ExecuteReaderAsync_CanBeCanceledWhileWaitingForConnectionGate()
    {
        using SqliteConnection connection = new("Data Source=:memory:");
        connection.Open();

        using (SqliteCommand setup = new("CREATE TABLE t(id INTEGER); INSERT INTO t(id) VALUES(1);", connection))
        {
            setup.ExecuteNonQuery();
        }

        using SqliteCommand blockingCommand = new("SELECT id FROM t;", connection);
        using SqliteDataReader blockingReader = (SqliteDataReader)blockingCommand.ExecuteReader();

        using SqliteCommand pendingCommand = new("SELECT id FROM t;", connection);
        using CancellationTokenSource cts = new();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pendingCommand.ExecuteReaderAsync(CommandBehavior.Default, cts.Token));
    }


    [Fact]
    public async Task ExecuteNonQueryAsync_CanBeCanceledWhileWaitingForConnectionGate()
    {
        using SqliteConnection connection = new("Data Source=:memory:");
        connection.Open();

        using (SqliteCommand setup = new("CREATE TABLE t(id INTEGER); INSERT INTO t(id) VALUES(1);", connection))
        {
            setup.ExecuteNonQuery();
        }

        using SqliteCommand blockingCommand = new("SELECT id FROM t;", connection);
        using SqliteDataReader blockingReader = (SqliteDataReader)blockingCommand.ExecuteReader();

        using SqliteCommand pendingCommand = new("INSERT INTO t(id) VALUES(2);", connection);
        using CancellationTokenSource cts = new();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pendingCommand.ExecuteNonQueryAsync(cts.Token));
    }

    [Fact]
    public async Task PrepareAsync_CanBeCanceledWhileWaitingForConnectionGate()
    {
        using SqliteConnection connection = new("Data Source=:memory:");
        connection.Open();

        using (SqliteCommand setup = new("CREATE TABLE t(id INTEGER); INSERT INTO t(id) VALUES(1);", connection))
        {
            setup.ExecuteNonQuery();
        }

        using SqliteCommand blockingCommand = new("SELECT id FROM t;", connection);
        using SqliteDataReader blockingReader = (SqliteDataReader)blockingCommand.ExecuteReader();

        using SqliteCommand pendingCommand = new("SELECT id FROM t;", connection);
        using CancellationTokenSource cts = new();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pendingCommand.PrepareAsync(cts.Token));
    }

}
