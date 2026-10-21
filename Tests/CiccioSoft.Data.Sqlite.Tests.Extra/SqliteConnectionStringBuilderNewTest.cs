// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections.Generic;
using CiccioSoft.Data.Sqlite.Properties;
using Xunit;

namespace CiccioSoft.Data.Sqlite;

public class SqliteConnectionStringBuilderNewTest
{
    [Theory, InlineData("Data Source"), InlineData("Filename"), InlineData("DataSource")]
    public void Ctor_parses_DataSource(string keyword)
    {
        var builder = new SqliteConnectionStringBuilder($"{keyword}=test.db");

        Assert.Equal("test.db", builder.DataSource);
    }

    [Fact]
    public void Ctor_parses_SharedCache()
    {
        Assert.Equal(SqliteCacheMode.Private, new SqliteConnectionStringBuilder("Cache=Private").Cache);
        Assert.Equal(SqliteCacheMode.Shared, new SqliteConnectionStringBuilder("Cache=Shared").Cache);
    }

    [Fact]
    public void Ctor_parses_mode()
    {
        var builder = new SqliteConnectionStringBuilder("Mode=Memory");

        Assert.Equal(SqliteOpenMode.Memory, builder.Mode);
    }

    [Fact]
    public void Ctor_parses_Password()
    {
        var builder = new SqliteConnectionStringBuilder("Password=key");

        Assert.Equal("key", builder.Password);
    }

    [Fact]
    public void Ctor_parses_ForeignKeys()
    {
        var builder = new SqliteConnectionStringBuilder("Foreign Keys=True");

        Assert.True(builder.ForeignKeys);
    }

    [Fact]
    public void Ctor_parses_RecursiveTriggers()
    {
        var builder = new SqliteConnectionStringBuilder("Recursive Triggers=True");

        Assert.True(builder.RecursiveTriggers);
    }

    [Theory, InlineData("Default Timeout"), InlineData("Command Timeout")]
    public void Ctor_parses_DefaultTimeout(string keyword)
    {
        var builder = new SqliteConnectionStringBuilder($"{keyword}=1");

        Assert.Equal(1, builder.DefaultTimeout);
    }

    [Fact]
    public void ConnectionString_defaults_to_empty()
    {
        var builder = new SqliteConnectionStringBuilder();

        Assert.Empty(builder.ToString());
    }

    [Fact]
    public void ConnectionString_coalesces_to_empty()
    {
        var builder = new SqliteConnectionStringBuilder { ConnectionString = null };

        Assert.NotNull(builder.ConnectionString);
        Assert.Empty(builder.ConnectionString);
    }

    [Fact]
    public void Filename_is_alias_for_DataSource()
    {
        var builder = new SqliteConnectionStringBuilder("Filename=inline.db");
        Assert.Equal("inline.db", builder.DataSource);
    }

    [Fact]
    public void It_takes_last_alias_specified()
    {
        var builder = new SqliteConnectionStringBuilder("Filename=ignore me.db; Data Source=and me too.db; DataSource=this_one.db");

        Assert.Equal("this_one.db", builder.DataSource);
    }

    [Fact]
    public void DataSource_works()
    {
        var builder = new SqliteConnectionStringBuilder();

        builder.DataSource = "test.db";

        Assert.Equal("test.db", builder.DataSource);
    }

    [Fact]
    public void DataSource_defaults_to_null()
        => Assert.Null(new SqliteConnectionStringBuilder().DataSource);

    [Fact]
    public void Mode_works()
    {
        var builder = new SqliteConnectionStringBuilder();

        builder.Mode = SqliteOpenMode.Memory;

        Assert.Equal(SqliteOpenMode.Memory, builder.Mode);
    }

    [Fact]
    public void Mode_defaults_to_null()
        => Assert.Null(new SqliteConnectionStringBuilder().Mode);

    [Fact]
    public void Cache_defaults_to_null()
        => Assert.Null(new SqliteConnectionStringBuilder().Cache);

    [Fact]
    public void Password_defaults_to_null()
        => Assert.Null(new SqliteConnectionStringBuilder().Password);

    [Fact]
    public void DefaultTimeout_defaults_to_null()
        => Assert.Null(new SqliteConnectionStringBuilder().DefaultTimeout);

    [Fact]
    public void Pooling_defaults_to_null()
        => Assert.Null(new SqliteConnectionStringBuilder().Pooling);

    [Fact]
    public void MaxPoolSize_defaults_to_null()
        => Assert.Null(new SqliteConnectionStringBuilder().MaxPoolSize);

    [Fact]
    public void Keys_works()
    {
        var keys = (ICollection<string>)new SqliteConnectionStringBuilder().Keys;

        Assert.True(keys.IsReadOnly);
        Assert.Equal(11, keys.Count);
        Assert.Contains("Data Source", keys);
        Assert.Contains("Mode", keys);
        Assert.Contains("Cache", keys);
        Assert.Contains("Password", keys);
        Assert.Contains("Foreign Keys", keys);
        Assert.Contains("Recursive Triggers", keys);
        Assert.Contains("Default Timeout", keys);
        Assert.Contains("Pooling", keys);
        Assert.Contains("Max Pool Size", keys);
        Assert.Contains("Journal Mode", keys);
        Assert.Contains("Vfs", keys);
    }

    [Fact]
    public void Values_works()
    {
        var values = (ICollection<object?>)new SqliteConnectionStringBuilder().Values;

        Assert.True(values.IsReadOnly);
        Assert.Equal(11, values.Count);
    }

    [Fact]
    public void Item_validates_argument()
    {
        var ex = Assert.Throws<ArgumentException>(() => new SqliteConnectionStringBuilder()["Invalid"]);
        Assert.Equal(Resources.KeywordNotSupported("Invalid"), ex.Message);

        ex = Assert.Throws<ArgumentException>(() => new SqliteConnectionStringBuilder()["Invalid"] = 0);
        Assert.Equal(Resources.KeywordNotSupported("Invalid"), ex.Message);
    }

    [Fact]
    public void Item_resets_value_when_null()
    {
        var builder = new SqliteConnectionStringBuilder();
        builder.DataSource = "test.db";

        builder["Data Source"] = null;

        Assert.Null(builder.DataSource);
    }

    [Fact]
    public void Item_gets_value()
    {
        var builder = new SqliteConnectionStringBuilder();
        builder.DataSource = "test.db";

        Assert.Equal("test.db", builder["Data Source"]);
    }

    [Fact]
    public void Item_sets_value()
    {
        var builder = new SqliteConnectionStringBuilder();

        builder["Data Source"] = "test.db";

        Assert.Equal("test.db", builder.DataSource);
    }

    [Theory, InlineData("Shared"), InlineData("SHARED"), InlineData(SqliteCacheMode.Shared), InlineData((int)SqliteCacheMode.Shared)]
    public void Item_converts_to_enum_on_set(object value)
    {
        var builder = new SqliteConnectionStringBuilder();

        builder["Cache"] = value;

        Assert.Equal(SqliteCacheMode.Shared, builder["Cache"]);
    }

    [Theory, InlineData(42), InlineData("Unknown"), InlineData((SqliteCacheMode)42), InlineData(SqliteOpenMode.ReadOnly)]
    public void Item_throws_when_cannot_convert_to_enum_on_set(object value)
    {
        var builder = new SqliteConnectionStringBuilder();

        Assert.ThrowsAny<ArgumentException>(() => builder["Cache"] = value);
    }

    [Theory, InlineData(1, true), InlineData("True", true), InlineData(0, false), InlineData("False", false), InlineData(null, null),
     InlineData("", null)]
    public void Item_converts_to_bool_on_set(object? value, bool? expected)
    {
        var builder = new SqliteConnectionStringBuilder();

        builder["Foreign Keys"] = value;

        Assert.Equal(expected, builder["Foreign Keys"]);
    }

    [Fact]
    public void Clear_resets_everything()
    {
        var builder = new SqliteConnectionStringBuilder(
            "Data Source=test.db;Mode=Memory;Cache=Shared;Password=test;Foreign Keys=True;Recursive Triggers=True;Default Timeout=1");

        builder.Clear();

        Assert.Null(builder.DataSource);
        Assert.Null(builder.Mode);
        Assert.Null(builder.Cache);
        Assert.Null(builder.Password);
        Assert.Null(builder.ForeignKeys);
        Assert.Null(builder.RecursiveTriggers);
        Assert.Null(builder.DefaultTimeout);
    }

    [Fact]
    public void ContainsKey_returns_true_when_exists()
        => Assert.True(new SqliteConnectionStringBuilder("Data Source=test.db").ContainsKey("Data Source"));

    [Fact]
    public void ContainsKey_returns_false_when_not_exists()
        => Assert.False(new SqliteConnectionStringBuilder().ContainsKey("Invalid"));

    [Fact]
    public void Remove_returns_false_when_not_exists()
        => Assert.False(new SqliteConnectionStringBuilder().Remove("Invalid"));

    [Fact]
    public void Remove_resets_option()
    {
        var builder = new SqliteConnectionStringBuilder("Data Source=test.db");

        var removed = builder.Remove("Data Source");

        Assert.True(removed);
        Assert.Null(builder.DataSource);
    }

    [Fact]
    public void ShouldSerialize_returns_false_when_not_exists()
        => Assert.False(new SqliteConnectionStringBuilder().ShouldSerialize("Invalid"));

    [Fact]
    public void ShouldSerialize_returns_false_when_unset()
        => Assert.False(new SqliteConnectionStringBuilder().ShouldSerialize("Data Source"));

    [Fact]
    public void ShouldSerialize_returns_true_when_set()
    {
        var builder = new SqliteConnectionStringBuilder("Data Source=test.db");

        Assert.True(builder.ShouldSerialize("Data Source"));
    }

    [Fact]
    public void TryGetValue_returns_false_when_not_exists()
    {
        var retrieved = new SqliteConnectionStringBuilder().TryGetValue("Invalid", out var value);

        Assert.False(retrieved);
        Assert.Null(value);
    }

    [Fact]
    public void TryGetValue_returns_true_when_exists()
    {
        var builder = new SqliteConnectionStringBuilder("Data Source=test.db");

        var retrieved = builder.TryGetValue("Data Source", out var value);

        Assert.True(retrieved);
        Assert.Equal("test.db", value);
    }

    [Fact]
    public void ToString_builds_string()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = "test.db",
            Cache = SqliteCacheMode.Shared,
            Mode = SqliteOpenMode.Memory,
            Password = "test",
            ForeignKeys = true,
            RecursiveTriggers = true,
            DefaultTimeout = 1
        };

        Assert.Equal(
            "Data Source=test.db;Mode=Memory;Cache=Shared;Password=test;Foreign Keys=True;Recursive Triggers=True;Default Timeout=1",
            builder.ToString());
    }

    [Fact]
    public void ToString_builds_minimal_string()
    {
        var builder = new SqliteConnectionStringBuilder { DataSource = "test.db" };

        Assert.Equal("Data Source=test.db", builder.ToString());
    }

    [Fact]
    public void Ctor_parses_known_values()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            ConnectionString = "Data Source=test.db;Pooling=false;Max Pool Size=42;Default Timeout=2500;Journal Mode=WAL;Foreign Keys=True;Recursive Triggers=False"
        };

        Assert.Equal("test.db", builder.DataSource);
        Assert.False(builder.Pooling);
        Assert.Equal(42, builder.MaxPoolSize);
        Assert.Equal(2500, builder.DefaultTimeout);
        Assert.Equal("WAL", builder.JournalMode);
        Assert.True(builder.ForeignKeys);
        Assert.False(builder.RecursiveTriggers);
    }

    [Fact]
    public void Setters_update_values()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = "file.db",
            Pooling = false,
            MaxPoolSize = 32,
            DefaultTimeout = 123,
            JournalMode = "MEMORY",
            ForeignKeys = true,
            RecursiveTriggers = false
        };

        Assert.Equal("file.db", builder["Data Source"]);
        Assert.False((bool?)builder["Pooling"]);
        Assert.Equal(32, builder["Max Pool Size"]);
        Assert.Equal(123, builder["Default Timeout"]);
        Assert.Equal("MEMORY", builder["Journal Mode"]);
        Assert.Equal(true, builder["Foreign Keys"]);
        Assert.Equal(false, builder["Recursive Triggers"]);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-10, 1)]
    [InlineData(5, 5)]
    public void MaxPoolSize_is_clamped_to_at_least_one(int input, int expected)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            MaxPoolSize = input
        };

        Assert.Equal(expected, builder.MaxPoolSize);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(5, 5)]
    public void DefaultTimeout_is_clamped_to_zero_or_greater(int input, int expected)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DefaultTimeout = input
        };

        Assert.Equal(expected, builder.DefaultTimeout);
    }

    [Fact]
    public void DefaultTimeout_parses_pragma_alias()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            ConnectionString = "Data Source=test.db;default_timeout=1500"
        };

        Assert.Equal(1500, builder.DefaultTimeout);
    }

    [Fact]
    public void ForeignKeys_parses_pragma_alias()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            ConnectionString = "Data Source=test.db;foreign_keys=0"
        };

        Assert.False(builder.ForeignKeys);
    }

    [Fact]
    public void JournalMode_parses_pragma_alias()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            ConnectionString = "Data Source=test.db;journal_mode=MEMORY"
        };

        Assert.Equal("MEMORY", builder.JournalMode);
    }

    [Fact]
    public void Keys_and_values_are_available()
    {
        var builder = new SqliteConnectionStringBuilder();

        var keys = (ICollection<string>)builder.Keys;
        var values = (ICollection<object?>)builder.Values;

        Assert.NotEmpty(keys);
        Assert.Equal(keys.Count, values.Count);
        Assert.Contains("Data Source", keys);
        Assert.Contains("Foreign Keys", keys);
    }
}
