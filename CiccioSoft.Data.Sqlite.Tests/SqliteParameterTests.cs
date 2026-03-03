// Copyright (c) 2026 Francesco Crimi
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Data;
using CiccioSoft.Data.Sqlite;
using Xunit;

namespace CiccioSoft.Data.Sqlite.Tests;

public class SqliteParameterTests
{
    [Fact]
    public void Ctor_sets_name_and_value()
    {
        var parameter = new SqliteParameter("@p", 12);

        Assert.Equal("@p", parameter.ParameterName);
        Assert.Equal(12, parameter.Value);
    }

    [Fact]
    public void Defaults_are_expected()
    {
        var parameter = new SqliteParameter();

        Assert.Equal(DbType.Object, parameter.DbType);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.Equal(string.Empty, parameter.ParameterName);
        Assert.Equal(string.Empty, parameter.SourceColumn);
    }

    [Fact]
    public void Direction_throws_on_invalid_value()
    {
        var parameter = new SqliteParameter();

        Assert.Throws<ArgumentException>(() => parameter.Direction = (ParameterDirection)12345);
    }

    [Fact]
    public void ResetDbType_restores_object()
    {
        var parameter = new SqliteParameter { DbType = DbType.Int64 };

        parameter.ResetDbType();

        Assert.Equal(DbType.Object, parameter.DbType);
    }

    [Fact]
    public void Null_name_and_source_column_become_empty()
    {
        var parameter = new SqliteParameter
        {
            ParameterName = null!,
            SourceColumn = null!
        };

        Assert.Equal(string.Empty, parameter.ParameterName);
        Assert.Equal(string.Empty, parameter.SourceColumn);
    }


    [Theory]
    [InlineData(ParameterDirection.Input)]
    [InlineData(ParameterDirection.Output)]
    [InlineData(ParameterDirection.InputOutput)]
    [InlineData(ParameterDirection.ReturnValue)]
    public void Direction_accepts_supported_values(ParameterDirection direction)
    {
        var parameter = new SqliteParameter();

        parameter.Direction = direction;

        Assert.Equal(direction, parameter.Direction);
    }

    [Fact]
    public void Value_allows_dbnull_roundtrip()
    {
        var parameter = new SqliteParameter { Value = DBNull.Value };

        Assert.Equal(DBNull.Value, parameter.Value);
    }

}
