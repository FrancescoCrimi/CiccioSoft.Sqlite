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
}
