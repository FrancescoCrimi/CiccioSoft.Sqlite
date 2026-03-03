using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using CiccioSoft.Data.Sqlite.Properties;

namespace CiccioSoft.Data.Sqlite;

public class SqliteParameter : DbParameter
{
    public SqliteParameter() { }

    public SqliteParameter(string name, object? value)
    {
        ParameterName = name;
        Value = value;
    }

    public override DbType DbType { get; set; } = DbType.Object;
    private ParameterDirection _direction = ParameterDirection.Input;
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
    private string _parameterName = string.Empty;
    private string _sourceColumn = string.Empty;

    public override string ParameterName
    {
        get => _parameterName;
        set => _parameterName = value ?? string.Empty;
    }

    public override int Size { get; set; }

    public override string SourceColumn
    {
        get => _sourceColumn;
        set => _sourceColumn = value ?? string.Empty;
    }
    public override bool SourceColumnNullMapping { get; set; }
    public override object? Value { get; set; }

    public override void ResetDbType() => DbType = DbType.Object;
}
