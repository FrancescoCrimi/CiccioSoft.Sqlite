// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using CiccioSoft.Data.Sqlite.Properties;

namespace CiccioSoft.Data.Sqlite;

public class SqliteParameterOld : DbParameter
{
    private ParameterDirection _direction = ParameterDirection.Input;
    private string _parameterName = string.Empty;
    private string _sourceColumn = string.Empty;

    public SqliteParameterOld() { }

    public SqliteParameterOld(string name, object? value)
    {
        ParameterName = name;
        Value = value;
    }

    public override DbType DbType { get; set; } = DbType.Object;

    public override ParameterDirection Direction
    {
        get => _direction;
        set => _direction = value switch
        {
            ParameterDirection.Input or
            ParameterDirection.Output or
            ParameterDirection.InputOutput or
            ParameterDirection.ReturnValue => value,
            _ => throw new ArgumentException(Resources.InvalidParameterDirection(value))
        };
    }

    public override bool IsNullable { get; set; }

    [DefaultValue("")]
    [AllowNull]
    public override string ParameterName
    {
        get => _parameterName;
        set => _parameterName = value ?? string.Empty;
    }

    public override int Size { get; set; }

    [DefaultValue("")]
    [AllowNull]
    public override string SourceColumn
    {
        get => _sourceColumn;
        set => _sourceColumn = value ?? string.Empty;
    }

    public override bool SourceColumnNullMapping { get; set; }

    public override object? Value { get; set; }

    public override void ResetDbType() => DbType = DbType.Object;
}
