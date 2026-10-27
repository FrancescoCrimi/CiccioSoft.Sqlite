// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections.Generic;
using Xunit;

namespace CiccioSoft.Data.Common;


public enum TestEnum
{
    ReadWriteCreate,
    ReadWrite,
    ReadOnly,
    Memory
}

public class TestConnectionStringBuilder : CommonDbConnectionStringBuilder
{
    private readonly ConnectionStringOption<string?> StringOption;
    private readonly ConnectionStringOption<bool?> BoolOption;
    private readonly ConnectionStringOption<int?> IntOption;
    private readonly ConnectionStringOption<TestEnum?> EnumOption;


    #region Ctor

    public TestConnectionStringBuilder() : base()
    {
        StringOption = Register<string?>(["String Option", "StringOption", "string_option"]);
        BoolOption = Register<bool?>(["Bool Option", "BoolOption", "bool_option"]);
        IntOption = Register<int?>(["Int Option", "IntOption", "int_option"]);
        EnumOption = Register<TestEnum?>(["Enum Option", "EnumOption", "enum_option"]);
    }

    public TestConnectionStringBuilder(string connectionString) : this()
    {
        if (!string.IsNullOrEmpty(connectionString))
        {
            ConnectionString = connectionString;
        }
    }

    #endregion


    #region Builder Property

    public string? StringProperty
    {
        get => StringOption.GetValue();
        set => StringOption.SetValue(value);
    }

    public bool? BoolProperty
    {
        get => BoolOption.GetValue();
        set => BoolOption.SetValue(value);
    }

    public int? IntProperty
    {
        get => IntOption.GetValue();
        set => IntOption.SetValue(value);
    }

    public virtual TestEnum? EnumProperty
    {
        get => EnumOption.GetValue();
        set => EnumOption.SetValue(value);
    }

    #endregion
}


public class CommonDbConnectionStringBuilderTests
{
    [Theory, InlineData(1, true), InlineData("True", true), InlineData(0, false), InlineData("False", false), InlineData(null, null),
     InlineData("", null)]
    public void Item_converts_to_bool_on_set(object? value, bool? expected)
    {
        var builder = new TestConnectionStringBuilder();

        builder["Bool Option"] = value;

        Assert.Equal(expected, builder["Bool Option"]);
    }

    [Fact]
    public void Clear_resets_everything()
    {
        var builder = new TestConnectionStringBuilder(
            "String Option=test.db;Bool Option=True;Int Option=1");

        builder.Clear();

        Assert.Null(builder.StringProperty);
        Assert.Null(builder.BoolProperty);
        Assert.Null(builder.IntProperty);
        Assert.Null(builder.EnumProperty);
    }

}
