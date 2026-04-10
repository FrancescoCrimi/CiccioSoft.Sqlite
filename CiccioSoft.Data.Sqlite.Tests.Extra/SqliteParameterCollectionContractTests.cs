// Copyright (c) 2026 Francesco Crimi
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections;
using System.Data.Common;
using CiccioSoft.Data.Sqlite;
using CiccioSoft.Data.Sqlite.Properties;
using Xunit;

namespace CiccioSoft.Data.Sqlite.Tests.Extra;

public class SqliteParameterCollectionContractTests
{
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

    [Fact]
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
}
