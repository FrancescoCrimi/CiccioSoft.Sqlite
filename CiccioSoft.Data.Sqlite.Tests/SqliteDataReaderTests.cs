// Copyright (c) 2026 Francesco Crimi
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Data;
using CiccioSoft.Data.Sqlite;
using Xunit;

namespace CiccioSoft.Data.Sqlite.Tests;

public class SqliteDataReaderTests
{
    [Fact]
    public void Read_and_GetValue_work_for_basic_types()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var command = new SqliteCommand("SELECT 1, 2.5, 'abc';", connection);
        using var reader = (SqliteDataReader)command.ExecuteReader();

        Assert.True(reader.Read());
        Assert.Equal(1L, reader.GetValue(0));
        Assert.Equal(2.5d, reader.GetValue(1));
        Assert.Equal("abc", reader.GetValue(2));
    }

    [Fact]
    public void GetOrdinal_is_case_insensitive()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var command = new SqliteCommand("SELECT 1 AS MyColumn;", connection);
        using var reader = (SqliteDataReader)command.ExecuteReader();

        Assert.True(reader.Read());
        Assert.Equal(0, reader.GetOrdinal("mycolumn"));
    }

    [Fact]
    public void GetValues_reads_all_visible_columns()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var command = new SqliteCommand("SELECT 5, 'x';", connection);
        using var reader = (SqliteDataReader)command.ExecuteReader();

        Assert.True(reader.Read());
        object[] values = new object[2];
        int count = reader.GetValues(values);

        Assert.Equal(2, count);
        Assert.Equal(5L, values[0]);
        Assert.Equal("x", values[1]);
    }

    [Fact]
    public void HasRows_is_false_for_empty_result()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var command = new SqliteCommand("SELECT 1 WHERE 0;", connection);
        using var reader = (SqliteDataReader)command.ExecuteReader();

        Assert.False(reader.HasRows);
        Assert.False(reader.Read());
    }

    [Fact]
    public void GetSchemaTable_returns_rows_for_result_columns()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var command = new SqliteCommand("SELECT 1 AS c1, 'a' AS c2;", connection);
        using var reader = (SqliteDataReader)command.ExecuteReader();

        DataTable schema = reader.GetSchemaTable();

        Assert.NotNull(schema);
        Assert.Equal(2, schema.Rows.Count);
    }


    [Fact]
    public void NextResult_iterates_multiple_resultsets()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var command = new SqliteCommand("SELECT 1 AS a; SELECT 2 AS b;", connection);
        using var reader = (SqliteDataReader)command.ExecuteReader();

        Assert.True(reader.Read());
        Assert.Equal(1L, reader.GetValue(0));
        Assert.True(reader.NextResult());
        Assert.True(reader.Read());
        Assert.Equal(2L, reader.GetValue(0));
        Assert.False(reader.NextResult());
    }

    [Fact]
    public void SingleResult_behavior_disables_nextresult()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var command = new SqliteCommand("SELECT 1; SELECT 2;", connection);
        using var reader = (SqliteDataReader)command.ExecuteReader(CommandBehavior.SingleResult);

        Assert.True(reader.Read());
        Assert.False(reader.NextResult());
    }

    [Fact]
    public void IsDBNull_detects_null_values()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var command = new SqliteCommand("SELECT NULL;", connection);
        using var reader = (SqliteDataReader)command.ExecuteReader();

        Assert.True(reader.Read());
        Assert.True(reader.IsDBNull(0));
        Assert.Equal(DBNull.Value, reader.GetValue(0));
    }

}
