using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using CiccioSoft.Data.Sqlite.Properties;

namespace CiccioSoft.Data.Sqlite;

internal sealed class SqliteParameterCollection : DbParameterCollection
{
    private readonly List<SqliteParameter> _items = new();
    private readonly object _syncRoot = new();

    public override int Count => _items.Count;
    public override object SyncRoot => _syncRoot;
    public override int Add(object value)
    {
        SqliteParameter parameter = AsSqliteParameter(value);
        ValidateParameterName(parameter);
        _items.Add(parameter);
        return _items.Count - 1;
    }

    public override void AddRange(Array values)
    {
        ArgumentNullException.ThrowIfNull(values);

        foreach (object? value in values)
        {
            Add(value!);
        }
    }

    public override void Clear() => _items.Clear();

    public override bool Contains(object value)
        => value is SqliteParameter parameter && _items.Contains(parameter);

    public override bool Contains(string value)
        => IndexOf(value) >= 0;

    public override void CopyTo(Array array, int index)
        => ((ICollection)_items).CopyTo(array, index);

    public override IEnumerator GetEnumerator()
        => _items.GetEnumerator();

    protected override DbParameter GetParameter(int index)
        => _items[index];

    protected override DbParameter GetParameter(string parameterName)
    {
        int index = IndexOf(parameterName);
        if (index < 0)
        {
            throw new IndexOutOfRangeException(Resources.ParameterNotFound(parameterName));
        }

        return _items[index];
    }

    public override int IndexOf(object value)
        => value is SqliteParameter parameter ? _items.IndexOf(parameter) : -1;

    public override int IndexOf(string parameterName)
    {
        if (string.IsNullOrEmpty(parameterName))
        {
            return -1;
        }

        string normalized = NormalizeParameterName(parameterName);
        for (int i = 0; i < _items.Count; i++)
        {
            if (NormalizeParameterName(_items[i].ParameterName).Equals(normalized, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    public override void Insert(int index, object value)
    {
        SqliteParameter parameter = AsSqliteParameter(value);
        ValidateParameterName(parameter);
        _items.Insert(index, parameter);
    }

    public override bool IsFixedSize => false;
    public override bool IsReadOnly => false;
    public override bool IsSynchronized => false;

    public override void Remove(object value)
    {
        if (value is SqliteParameter parameter)
        {
            _items.Remove(parameter);
        }
    }

    public override void RemoveAt(int index)
        => _items.RemoveAt(index);

    public override void RemoveAt(string parameterName)
    {
        int index = IndexOf(parameterName);
        if (index < 0)
        {
            throw new IndexOutOfRangeException(Resources.ParameterNotFound(parameterName));
        }

        _items.RemoveAt(index);
    }

    protected override void SetParameter(int index, DbParameter value)
    {
        SqliteParameter parameter = AsSqliteParameter(value);
        ValidateParameterName(parameter);
        _items[index] = parameter;
    }

    protected override void SetParameter(string parameterName, DbParameter value)
    {
        SqliteParameter parameter = AsSqliteParameter(value);
        int index = IndexOf(parameterName);
        if (index < 0)
        {
            Add(parameter);
            return;
        }

        ValidateParameterName(parameter);
        _items[index] = parameter;
    }

    private SqliteParameter AsSqliteParameter(object value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value is not SqliteParameter parameter)
        {
            throw new InvalidCastException($"The SqliteParameterCollection only accepts non-null {nameof(SqliteParameter)} objects.");
        }

        return parameter;
    }

    private static void ValidateParameterName(SqliteParameter parameter)
    {
        string name = parameter.ParameterName;
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("ParameterName cannot consist only of white-space characters.", nameof(parameter));
        }

        string normalized = NormalizeParameterName(name);
        if (normalized.Length == 0)
        {
            throw new ArgumentException("ParameterName must contain at least one non-prefix character.", nameof(parameter));
        }
    }

    private static string NormalizeParameterName(string parameterName)
    {
        string trimmed = parameterName.Trim();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        return trimmed[0] is '@' or ':' or '$'
            ? trimmed.Substring(1)
            : trimmed;
    }
}
