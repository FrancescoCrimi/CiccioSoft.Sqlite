// Copyright (c) 2026 Francesco Crimi
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Data;
using CiccioSoft.Data.Sqlite;
using Xunit;

namespace CiccioSoft.Data.Sqlite.Tests;

public class SqliteCommandTests
{
    [Fact]
    public void Ctor_sets_command_text_and_connection()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        using var command = new SqliteCommand("SELECT 1;", connection);

        Assert.Equal("SELECT 1;", command.CommandText);
        Assert.Same(connection, command.Connection);
    }

    [Fact]
    public void CommandTimeout_throws_when_negative()
    {
        using var command = new SqliteCommand();

        Assert.Throws<ArgumentOutOfRangeException>(() => command.CommandTimeout = -1);
    }

    [Fact]
    public void ExecuteScalar_returns_first_column_of_first_row()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var command = new SqliteCommand("SELECT 42, 99;", connection);
        var result = command.ExecuteScalar();

        Assert.Equal(42L, result);
    }

    [Fact]
    public void ExecuteNonQuery_returns_changes()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using (var create = new SqliteCommand("CREATE TABLE t (id INTEGER);", connection))
        {
            create.ExecuteNonQuery();
        }

        using var insert = new SqliteCommand("INSERT INTO t VALUES (1);", connection);
        int changes = insert.ExecuteNonQuery();

        Assert.Equal(1, changes);
    }

    [Fact]
    public void ExecuteReader_binds_parameters_by_name()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var command = new SqliteCommand("SELECT $a, :b;", connection);
        command.Parameters.Add(new SqliteParameter("@a", 7));
        command.Parameters.Add(new SqliteParameter("b", "ok"));

        using var reader = (SqliteDataReader)command.ExecuteReader(CommandBehavior.SingleRow);
        Assert.True(reader.Read());
        Assert.Equal(7L, reader.GetValue(0));
        Assert.Equal("ok", reader.GetValue(1));
    }


    [Fact]
    public void CommandType_rejects_non_text()
    {
        using var command = new SqliteCommand();

        Assert.Throws<NotSupportedException>(() => command.CommandType = CommandType.StoredProcedure);
    }

    [Fact]
    public void ExecuteNonQuery_throws_when_connection_not_open()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        using var command = new SqliteCommand("SELECT 1;", connection);

        Assert.Throws<InvalidOperationException>(() => command.ExecuteNonQuery());
    }

    [Fact]
    public void Prepare_throws_when_connection_not_open()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        using var command = new SqliteCommand("SELECT 1;", connection);

        Assert.Throws<InvalidOperationException>(() => command.Prepare());
    }

}
