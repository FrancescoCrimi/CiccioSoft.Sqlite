// Copyright (c) 2026 Francesco Crimi
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Data;
using CiccioSoft.Data.Sqlite;
using Xunit;

namespace CiccioSoft.Data.Sqlite.Tests.Extra;

public class SqliteCommandReaderBehaviorTests
{
    [Fact]
    public void CommandText_can_be_changed_while_reader_keeps_original_batch()
    {
        using SqliteConnection connection = new("Data Source=:memory:");
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT 1; SELECT 2;";

        using (SqliteDataReader reader = command.ExecuteReader())
        {
            Assert.True(reader.Read());
            Assert.Equal(1L, reader.GetInt64(0));

            command.CommandText = "SELECT 3;";
            Assert.Equal("SELECT 3;", command.CommandText);

            Assert.True(reader.NextResult());
            Assert.True(reader.Read());
            Assert.Equal(2L, reader.GetInt64(0));
        }

        Assert.Equal(3L, command.ExecuteScalar());
    }

    [Fact]
    public void Connection_can_be_changed_while_reader_keeps_original_connection()
    {
        using SqliteConnection connection = new("Data Source=:memory:");
        using SqliteConnection replacementConnection = new("Data Source=:memory:");
        connection.Open();
        replacementConnection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT 1 UNION ALL SELECT 2";

        using (SqliteDataReader reader = command.ExecuteReader(CommandBehavior.CloseConnection))
        {
            Assert.True(reader.Read());
            Assert.Equal(1L, reader.GetInt64(0));

            command.Connection = replacementConnection;

            Assert.Same(replacementConnection, command.Connection);
            Assert.True(reader.Read());
            Assert.Equal(2L, reader.GetInt64(0));
        }

        Assert.Equal(ConnectionState.Closed, connection.State);
        Assert.Equal(ConnectionState.Open, replacementConnection.State);
    }

    [Fact]
    public void ExecuteReader_allows_multiple_independent_readers_on_same_command()
    {
        using SqliteConnection connection = new("Data Source=:memory:");
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT 1;";

        using SqliteDataReader firstReader = command.ExecuteReader();
        using SqliteDataReader secondReader = command.ExecuteReader();

        Assert.True(firstReader.Read());
        Assert.True(secondReader.Read());
        Assert.Equal(1L, firstReader.GetInt64(0));
        Assert.Equal(1L, secondReader.GetInt64(0));
    }
}
