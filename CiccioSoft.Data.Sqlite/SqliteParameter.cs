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
using CiccioSoft.Sqlite.Interop;
using CiccioSoft.Data.Sqlite.Properties;

namespace CiccioSoft.Data.Sqlite;

/// <summary>
///     Represents a parameter and its value in a <see cref="SqliteCommand" />.
/// </summary>
/// <remarks>Due to SQLite's dynamic type system, parameter values are not converted.</remarks>
/// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/parameters">Parameters</seealso>
/// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/types">Data Types</seealso>
public class SqliteParameter : DbParameter
{
    private string _parameterName = string.Empty;
    private object? _value;
    private int? _size;
    private SqliteType? _sqliteType;
    private string _sourceColumn = string.Empty;
    private ParameterDirection _direction = ParameterDirection.Input;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SqliteParameter" /> class.
    /// </summary>
    /// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/parameters">Parameters</seealso>
    public SqliteParameter()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SqliteParameter" /> class.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="value">The value of the parameter. Can be null.</param>
    /// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/parameters">Parameters</seealso>
    /// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/types">Data Types</seealso>
    public SqliteParameter(string? name, object? value)
    {
        ParameterName = name;
        Value = value;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SqliteParameter" /> class.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="type">The type of the parameter.</param>
    /// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/parameters">Parameters</seealso>
    public SqliteParameter(string? name, SqliteType type)
    {
        ParameterName = name;
        SqliteType = type;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SqliteParameter" /> class.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="type">The type of the parameter.</param>
    /// <param name="size">The maximum size, in bytes, of the parameter.</param>
    /// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/parameters">Parameters</seealso>
    public SqliteParameter(string? name, SqliteType type, int size)
        : this(name, type)
        => Size = size;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SqliteParameter" /> class.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="type">The type of the parameter.</param>
    /// <param name="size">The maximum size, in bytes, of the parameter.</param>
    /// <param name="sourceColumn">The source column used for loading the value. Can be null.</param>
    /// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/parameters">Parameters</seealso>
    public SqliteParameter(string? name, SqliteType type, int size, string? sourceColumn)
        : this(name, type, size)
        => SourceColumn = sourceColumn;

    /// <summary>
    ///     Gets or sets the type of the parameter.
    /// </summary>
    /// <value>The type of the parameter.</value>
    /// <remarks>Due to SQLite's dynamic type system, parameter values are not converted.</remarks>
    public override DbType DbType { get; set; } = DbType.Object;

    /// <summary>
    ///     Gets or sets the SQLite type of the parameter.
    /// </summary>
    /// <value>The SQLite type of the parameter.</value>
    /// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/parameters">Parameters</seealso>
    public virtual SqliteType SqliteType
    {
        get => _sqliteType ?? SqliteValueBinder.GetSqliteType(_value);
        set => _sqliteType = value;
    }

    /// <summary>
    ///     Gets or sets the direction of the parameter. Only <see cref="ParameterDirection.Input" /> is supported.
    /// </summary>
    /// <value>The direction of the parameter.</value>
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

    /// <summary>
    ///     Gets or sets a value indicating whether the parameter is nullable.
    /// </summary>
    /// <value>A value indicating whether the parameter is nullable.</value>
    public override bool IsNullable { get; set; }

    /// <summary>
    ///     Gets or sets the name of the parameter.
    /// </summary>
    /// <value>The name of the parameter.</value>
    [DefaultValue("")]
    [AllowNull]
    public override string ParameterName
    {
        get => _parameterName;
        set => _parameterName = value ?? string.Empty;
    }

    public override int Size
    {
        // get;
        // set;
        get => _size
            ?? (_value is string stringValue
                ? stringValue.Length
                : _value is byte[] byteArray
                    ? byteArray.Length
                    : 0);

        set
        {
            if (value < -1)
            {
                // NB: Message is provided by the framework
                throw new ArgumentOutOfRangeException(nameof(value), value, message: null);
            }

            _size = value;
        }
    }

    /// <summary>
    ///     Gets or sets the source column used for loading the value.
    /// </summary>
    /// <value>The source column used for loading the value.</value>
    [DefaultValue("")]
    [AllowNull]
    public override string SourceColumn
    {
        get => _sourceColumn;
        set => _sourceColumn = value ?? string.Empty;
    }

    /// <summary>
    ///     Gets or sets a value indicating whether the source column is nullable.
    /// </summary>
    /// <value>A value indicating whether the source column is nullable.</value>
    public override bool SourceColumnNullMapping { get; set; }

    /// <summary>
    ///     Gets or sets the value of the parameter.
    /// </summary>
    /// <value>The value of the parameter.</value>
    /// <remarks>Due to SQLite's dynamic type system, parameter values are not converted.</remarks>
    /// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/types">Data Types</seealso>
    public override object? Value
    {
        get => _value;
        set => _value = value;
    }

    /// <summary>
    ///     Resets the <see cref="DbType" /> property to its original value.
    /// </summary>
    public override void ResetDbType()
        => ResetSqliteType();
    // => DbType = DbType.Object;

    /// <summary>
    ///     Resets the <see cref="SqliteType" /> property to its original value.
    /// </summary>
    public virtual void ResetSqliteType()
    {
        DbType = DbType.Object;
        _sqliteType = null;
    }
}
