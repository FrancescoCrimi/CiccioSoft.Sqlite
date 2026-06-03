// Copyright (c) 2026 Francesco Crimi
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Data;
using CiccioSoft.Data.Sqlite.Properties;
using Xunit;

namespace CiccioSoft.Data.Sqlite;

public class SqliteParameterDirectionBehaviorTests
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
