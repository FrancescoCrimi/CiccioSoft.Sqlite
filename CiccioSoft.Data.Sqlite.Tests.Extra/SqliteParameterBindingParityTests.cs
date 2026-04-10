// Copyright (c) 2026 Francesco Crimi
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using CiccioSoft.Data.Sqlite;
using Xunit;

namespace CiccioSoft.Data.Sqlite.Tests.Extra;

public class SqliteParameterBindingParityTests
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
}
