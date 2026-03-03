using System;
using System.Data;
using CiccioSoft.Data.Sqlite;
using CiccioSoft.Data.Sqlite.Properties;
using Xunit;

namespace CiccioSoft.Data.Sqlite.Tests;

public class SqliteParameterDirectionBehaviorTest
{
    [Theory]
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
