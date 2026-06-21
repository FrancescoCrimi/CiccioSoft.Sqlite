using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using CiccioSoft.Data.Sqlite.Properties;
using Xunit;

namespace CiccioSoft.Data.Sqlite;

public class SqliteParameterTest
{
    [Fact]
    public void Bind_ResolvesAcrossPrefixStyles()
    {
        using SqliteConnection connection = new("Data Source=:memory:;");
        connection.Open();

        using SqliteCommand command = new("SELECT $value, :other;", connection);
        command.Parameters.Add(new SqliteParameter("@value", 123));
        command.Parameters.Add(new SqliteParameter("other", "ok"));

        using SqliteDataReader reader = (SqliteDataReader)command.ExecuteReader();
        Assert.True(reader.Read());
        Assert.Equal(123L, reader.GetValue(0));
        Assert.Equal("ok", reader.GetValue(1));
    }

    [Fact]
    public void Bind_SupportsNullAndBlob()
    {
        using SqliteConnection connection = new("Data Source=:memory:;");
        connection.Open();

        using SqliteCommand command = new("SELECT @n, @b;", connection);
        byte[] expected = new byte[] { 1, 2, 3 };
        command.Parameters.Add(new SqliteParameter("@n", DBNull.Value));
        command.Parameters.Add(new SqliteParameter("@b", expected));

        using SqliteDataReader reader = (SqliteDataReader)command.ExecuteReader();
        Assert.True(reader.Read());
        Assert.Equal(DBNull.Value, reader.GetValue(0));
        Assert.Equal(expected, (byte[])reader.GetValue(1));
    }

    [Fact]
    public void Bind_FormatsGuidAndDateTimeInvariantly()
    {
        using SqliteConnection connection = new("Data Source=:memory:;");
        connection.Open();

        Guid guid = new("1c902ddb-f4b6-4945-af38-0dc1b0760465");
        DateTime dateTime = new(2014, 4, 14, 11, 13, 59, DateTimeKind.Unspecified);

        using SqliteCommand command = new("SELECT @g, @d;", connection);
        command.Parameters.Add(new SqliteParameter("@g", guid));
        command.Parameters.Add(new SqliteParameter("@d", dateTime));

        using SqliteDataReader reader = (SqliteDataReader)command.ExecuteReader();
        Assert.True(reader.Read());
        Assert.Equal("1C902DDB-F4B6-4945-AF38-0DC1B0760465", reader.GetValue(0));
        Assert.Equal("2014-04-14 11:13:59", reader.GetValue(1));
    }

    [Fact]
    public void Add_validates_null()
    {
        var command = new SqliteCommand();
        var parameters = (IList)command.Parameters;

        Assert.Throws<ArgumentNullException>(() => parameters.Add(null!));
    }

    [Fact]
    public void Add_validates_type()
    {
        var command = new SqliteCommand();
        var parameters = (IList)command.Parameters;

        Assert.Throws<InvalidCastException>(() => parameters.Add("not-a-parameter"));
    }

    [Fact]
    public void Add_allows_same_core_name_with_different_prefixes()
    {
        var command = new SqliteCommand();
        DbParameterCollection parameters = command.Parameters;

        parameters.Add(new SqliteParameter { ParameterName = "@Id", Value = 1 });
        parameters.Add(new SqliteParameter { ParameterName = ":id", Value = 2 });

        Assert.Equal(2, parameters.Count);
    }

    [Fact(Skip = "Fail on Microsoft.Data.Sqlite")]
    public void Add_rejects_invalid_edge_case_names()
    {
        var command = new SqliteCommand();
        DbParameterCollection parameters = command.Parameters;

        Assert.Throws<ArgumentException>(() => parameters.Add(new SqliteParameter { ParameterName = "   ", Value = 1 }));
        Assert.Throws<ArgumentException>(() => parameters.Add(new SqliteParameter { ParameterName = "@", Value = 1 }));
    }

    [Fact]
    public void RemoveAt_by_name_throws_if_not_found_with_provider_message()
    {
        var command = new SqliteCommand();
        DbParameterCollection parameters = command.Parameters;

        var ex = Assert.Throws<IndexOutOfRangeException>(() => parameters.RemoveAt("@missing"));

        Assert.Equal(Resources.ParameterNotFound("@missing"), ex.Message);
    }

    [Fact]
    public void AddRange_keeps_successfully_added_items_before_failure()
    {
        var command = new SqliteCommand();
        DbParameterCollection parameters = command.Parameters;

        var values = new object[]
        {
            new SqliteParameter { ParameterName = "@one", Value = 1 },
            "bad-value"
        };

        Assert.Throws<InvalidCastException>(() => parameters.AddRange(values));
        Assert.Single(command.Parameters);
        Assert.Equal("@one", command.Parameters[0].ParameterName);
    }

    [Theory(Skip = "Rompe la compatibilità com MSSqlite, vecchio paradigma")]
    [InlineData(ParameterDirection.Output)]
    [InlineData(ParameterDirection.InputOutput)]
    [InlineData(ParameterDirection.ReturnValue)]
    public void Execute_Rejects_NonInput_Directions(ParameterDirection direction)
    {
        using SqliteConnection connection = new("Data Source=:memory:;");
        connection.Open();

        using SqliteCommand command = new("SELECT @value;", connection);
        command.Parameters.Add(new SqliteParameter("@value", 1) { Direction = direction });

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => command.ExecuteScalar());

        Assert.Equal(Resources.InvalidParameterDirection(direction), ex.Message);
    }

    [Fact]
    public void Execute_Accepts_Input_Direction()
    {
        using SqliteConnection connection = new("Data Source=:memory:;");
        connection.Open();

        using SqliteCommand command = new("SELECT @value;", connection);
        command.Parameters.Add(new SqliteParameter("@value", 1) { Direction = ParameterDirection.Input });

        object? result = command.ExecuteScalar();

        Assert.Equal(1L, result);
    }

}