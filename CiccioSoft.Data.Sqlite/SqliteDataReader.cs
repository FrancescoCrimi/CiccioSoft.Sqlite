// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CiccioSoft.Data.Sqlite.Properties;
using CiccioSoft.Sqlite.Interop;

namespace CiccioSoft.Data.Sqlite;

public class SqliteDataReader : DbDataReader
{
    private const string SchemaDataTypeNameColumn = "DataTypeName";
    private const string SchemaIsKeyColumn = "IsKey";
    private const string SchemaIsUniqueColumn = "IsUnique";

    private readonly SqliteCommand _command;
    private readonly SqliteSession _session;
    private readonly System.Data.CommandBehavior _behavior;
    private readonly SqliteCommand.CommandExecutionScope _executionScope;
    private readonly SqliteCommand.BatchExecutionState _batchState;
    private Sqlite3Stmt? _stmt;
    private bool _hasRow;
    private bool _prefetched;
    private bool _readStarted;
    private bool _closed;
    private int _recordsAffected = -1;

    private SqliteDataReader(
        SqliteCommand command,
        SqliteSession session,
        System.Data.CommandBehavior behavior,
        Sqlite3Stmt? stmt,
        SqliteCommand.BatchExecutionState batchState,
        SqliteCommand.CommandExecutionScope executionScope)
    {
        _command = command;
        _session = session;
        _behavior = behavior;
        _stmt = stmt;
        _batchState = batchState;
        _executionScope = executionScope;
    }

    internal static SqliteDataReader Create(SqliteCommand command, SqliteSession session, System.Data.CommandBehavior behavior)
    {
        session.Gate.Wait();
        try
        {
            SqliteCommand.CommandExecutionScope scope = command.CreateExecutionScope(session, CancellationToken.None);
            SqliteCommand.BatchExecutionState batchState = new(command.CommandText);
            Sqlite3Stmt? stmt;
            try
            {
                stmt = scope.Execute(() => command.PrepareAndBindNext(session, batchState));
            }
            catch
            {
                scope.Dispose();
                throw;
            }

            return new SqliteDataReader(command, session, behavior, stmt, batchState, scope);
        }
        catch
        {
            session.Gate.Release();
            throw;
        }
    }

    internal static async Task<SqliteDataReader> CreateAsync(SqliteCommand command, SqliteSession session, System.Data.CommandBehavior behavior, CancellationToken cancellationToken)
    {
        await session.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            SqliteCommand.CommandExecutionScope scope = command.CreateExecutionScope(session, cancellationToken);
            SqliteCommand.BatchExecutionState batchState = new(command.CommandText);
            Sqlite3Stmt? stmt;
            try
            {
                stmt = scope.Execute(() => command.PrepareAndBindNext(session, batchState));
            }
            catch
            {
                scope.Dispose();
                throw;
            }

            return new SqliteDataReader(command, session, behavior, stmt, batchState, scope);
        }
        catch
        {
            session.Gate.Release();
            throw;
        }
    }


    private Sqlite3Stmt Stmt => _stmt ?? throw new InvalidOperationException(Resources.NoData);
    public override object this[int ordinal] => GetValue(ordinal);
    public override object this[string name] => GetValue(GetOrdinal(name));
    public override int Depth => 0;
    public override int FieldCount
    {
        get
        {
            EnsureOpen();
            return _stmt?.ColumnCount() ?? 0;
        }
    }
    public override bool HasRows
    {
        get
        {
            EnsureOpen();
            EnsurePrefetched();
            return _hasRow;
        }
    }

    public override bool IsClosed => _closed;
    public override int RecordsAffected => _recordsAffected;

    public override bool GetBoolean(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        return Stmt.GetInt(ordinal) != 0;
    }

    public override byte GetByte(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        return (byte)Stmt.GetInt(ordinal);
    }

    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        ReadOnlySpan<byte> blob = Stmt.GetBlob(ordinal);
        if (dataOffset < 0 || dataOffset > blob.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(dataOffset));
        }

        if (buffer is null)
        {
            return blob.Length;
        }

        int available = Math.Max(0, blob.Length - (int)dataOffset);
        int toCopy = Math.Min(length, available);
        if (toCopy > 0)
        {
            blob.Slice((int)dataOffset, toCopy).CopyTo(buffer.AsSpan(bufferOffset, toCopy));
        }

        return available;
    }

    public override char GetChar(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        return Stmt.GetColumnType(ordinal) switch
        {
            1 => (char)Stmt.GetInt(ordinal),
            2 => (char)Convert.ToInt32(Stmt.GetDouble(ordinal), CultureInfo.InvariantCulture),
            3 => (Stmt.GetString(ordinal) ?? throw new InvalidOperationException(Resources.CalledOnNullValue(ordinal)))[0],
            _ => throw new InvalidCastException(),
        };
    }

    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        string s = GetString(ordinal);
        if (dataOffset < 0 || dataOffset > s.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(dataOffset));
        }

        if (buffer is null)
        {
            return s.Length;
        }

        int available = Math.Max(0, s.Length - (int)dataOffset);
        int toCopy = Math.Min(length, available);
        if (toCopy > 0)
        {
            s.AsSpan((int)dataOffset, toCopy).CopyTo(buffer.AsSpan(bufferOffset, toCopy));
        }

        return available;
    }

    public override string GetDataTypeName(int ordinal)
    {
        EnsureOpen();
        if (FieldCount == 0)
        {
            throw new InvalidOperationException(Resources.NoData);
        }

        ValidateOrdinal(ordinal);

        string declaredTypeName = GetDataTypeNameFromDeclaration(ordinal);
        if (!string.Equals(declaredTypeName, "BLOB", StringComparison.Ordinal))
        {
            return declaredTypeName;
        }

        if (!_readStarted)
        {
            EnsurePrefetched();
        }

        return Stmt.GetColumnType(ordinal) switch
        {
            1 => "INTEGER",
            2 => "REAL",
            3 => "TEXT",
            4 => "BLOB",
            _ => declaredTypeName,
        };
    }

    public override DateTime GetDateTime(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        return Stmt.GetColumnType(ordinal) switch
        {
            1 => JulianDayToDateTime(Stmt.GetLong(ordinal)),
            2 => JulianDayToDateTime(Stmt.GetDouble(ordinal)),
            3 => DateTime.Parse(GetString(ordinal), CultureInfo.InvariantCulture),
            5 => throw new InvalidOperationException(Resources.CalledOnNullValue(ordinal)),
            _ => throw new InvalidCastException(),
        };
    }

    public override decimal GetDecimal(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        object value = GetValue(ordinal);
        if (value is string text)
        {
            return decimal.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
    }
    public override double GetDouble(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);
        if (IsDBNull(ordinal))
        {
            throw new InvalidOperationException(Resources.CalledOnNullValue(ordinal));
        }

        return Stmt.GetDouble(ordinal);
    }
    public override IEnumerator GetEnumerator() => new DbEnumerator(this);

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
    public override Type GetFieldType(int ordinal)
    {
        EnsureOpen();
        if (FieldCount == 0)
        {
            throw new InvalidOperationException(Resources.NoData);
        }

        ValidateOrdinal(ordinal);
        EnsurePrefetched();

        return InferFieldType(ordinal);
    }

    public override float GetFloat(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        return (float)Stmt.GetDouble(ordinal);
    }

    public override T GetFieldValue<T>(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        object value = GetValue(ordinal);
        Type targetType = typeof(T);
        Type nonNullableType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (value is DBNull)
        {
            if (targetType == typeof(DBNull))
            {
                return (T)value;
            }

            throw new InvalidOperationException(Resources.CalledOnNullValue(ordinal));
        }

        if (targetType == typeof(Stream))
        {
            byte[] bytes = value is byte[] blob
                ? blob
                : System.Text.Encoding.UTF8.GetBytes(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
            return (T)(object)new MemoryStream(bytes, writable: false);
        }

        if (targetType == typeof(TextReader))
        {
            return (T)(object)new StreamReader(new MemoryStream(System.Text.Encoding.UTF8.GetBytes((string)value), writable: false), System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: false);
        }

        if (nonNullableType == typeof(Guid))
        {
            if (value is byte[] bytes)
            {
                return (T)(object)new Guid(bytes);
            }

            return (T)(object)Guid.Parse((string)value);
        }

        if (nonNullableType == typeof(DateTimeOffset))
        {
            if (value is string s)
            {
                return (T)(object)DateTimeOffset.Parse(s, CultureInfo.InvariantCulture);
            }

            return (T)(object)new DateTimeOffset(GetDateTime(ordinal));
        }

        if (nonNullableType == typeof(TimeSpan))
        {
            return (T)(object)TimeSpan.Parse((string)value, CultureInfo.InvariantCulture);
        }

        if (nonNullableType == typeof(DateOnly))
        {
            DateTime dateTime = value switch
            {
                long l => JulianDayToDateTime(l),
                double d => JulianDayToDateTime(d),
                string s => DateTime.Parse(s, CultureInfo.InvariantCulture),
                _ => throw new InvalidCastException(),
            };

            return (T)(object)DateOnly.FromDateTime(dateTime);
        }

        if (nonNullableType == typeof(TimeOnly))
        {
            TimeOnly timeOnly = value switch
            {
                string s => TimeOnly.Parse(s, CultureInfo.InvariantCulture),
                _ => TimeOnly.FromDateTime(GetDateTime(ordinal)),
            };

            return (T)(object)timeOnly;
        }

        if (nonNullableType == typeof(decimal))
        {
            return (T)(object)Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }

        if (nonNullableType.IsEnum)
        {
            object enumValue = Enum.ToObject(nonNullableType, Convert.ChangeType(value, Enum.GetUnderlyingType(nonNullableType), CultureInfo.InvariantCulture));
            return (T)enumValue;
        }

        if (value is T cast)
        {
            return cast;
        }

        object converted = Convert.ChangeType(value, nonNullableType, CultureInfo.InvariantCulture);
        return (T)converted;
    }

    public override Guid GetGuid(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        return Guid.Parse(GetString(ordinal));
    }

    public override short GetInt16(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        return (short)Stmt.GetInt(ordinal);
    }
    public override int GetInt32(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        return Stmt.GetInt(ordinal);
    }

    public override long GetInt64(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);
        if (IsDBNull(ordinal))
        {
            throw new InvalidOperationException(Resources.CalledOnNullValue(ordinal));
        }

        return Stmt.GetLong(ordinal);
    }

    public override string GetName(int ordinal)
    {
        EnsureOpen();
        if (FieldCount == 0)
        {
            throw new InvalidOperationException(Resources.NoData);
        }

        ValidateOrdinal(ordinal);

        return Stmt.GetColumnName(ordinal) ?? string.Empty;
    }

    public override int GetOrdinal(string name)
    {
        EnsureOpen();
        if (FieldCount == 0)
        {
            throw new InvalidOperationException(Resources.NoData);
        }

        ArgumentNullException.ThrowIfNull(name);

        int insensitiveIndex = -1;
        string? insensitiveName = null;

        for (int i = 0; i < FieldCount; i++)
        {
            string columnName = GetName(i);
            if (string.Equals(columnName, name, StringComparison.Ordinal))
            {
                return i;
            }

            if (!string.Equals(columnName, name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (insensitiveIndex >= 0)
            {
                throw new InvalidOperationException(Resources.AmbiguousColumnName(name, insensitiveName ?? string.Empty, columnName));
            }

            insensitiveIndex = i;
            insensitiveName = columnName;
        }

        if (insensitiveIndex >= 0)
        {
            return insensitiveIndex;
        }

        throw new ArgumentOutOfRangeException(nameof(name), name, $"Column '{name}' not found.");
    }

    public override string GetString(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        string? value = Stmt.GetString(ordinal);
        if (value is null)
        {
            throw new InvalidOperationException(Resources.CalledOnNullValue(ordinal));
        }

        return value;
    }

    public override Stream GetStream(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        if (IsDBNull(ordinal))
        {
            throw new InvalidOperationException(Resources.CalledOnNullValue(ordinal));
        }

        object value = GetValue(ordinal);
        byte[] bytes = value is byte[] blob
            ? blob
            : System.Text.Encoding.UTF8.GetBytes(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
        return new MemoryStream(bytes, writable: false);
    }

    public override TextReader GetTextReader(int ordinal)
    {
        if (IsDBNull(ordinal))
        {
            return new StringReader(string.Empty);
        }

        return GetFieldValue<TextReader>(ordinal);
    }

    public override object GetValue(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        if (IsDBNull(ordinal)) return DBNull.Value;
        return Stmt.GetColumnType(ordinal) switch
        {
            1 => Stmt.GetLong(ordinal),
            2 => Stmt.GetDouble(ordinal),
            3 => Stmt.GetString(ordinal) ?? string.Empty,
            4 => Stmt.GetBlob(ordinal).ToArray(),
            _ => DBNull.Value,
        };
    }

    public override int GetValues(object[] values)
    {
        EnsureHasRow();
        ArgumentNullException.ThrowIfNull(values);
        if (values.Length < FieldCount)
        {
            throw new IndexOutOfRangeException();
        }

        int count = Math.Min(values.Length, FieldCount);
        for (int i = 0; i < count; i++) values[i] = GetValue(i);
        return count;
    }


    public override bool IsDBNull(int ordinal)
    {
        EnsureHasRow();
        return Stmt.GetColumnType(ordinal) == 5;
    }
    public override bool NextResult()
    {
        EnsureOpen();

        if (_behavior.HasFlag(System.Data.CommandBehavior.SingleResult))
        {
            return false;
        }

        while (true)
        {
            AddChangesFromCurrentStatement();
            _stmt?.Dispose();
            _prefetched = false;
            _readStarted = false;
            _hasRow = false;

            Sqlite3Stmt? next = _executionScope.Execute(() => _command.PrepareAndBindNext(_session, _batchState));
            if (next is null)
            {
                return false;
            }

            _stmt = next;
            if (_stmt is not null && Stmt.ColumnCount() > 0)
            {
                return true;
            }

            while (_executionScope.Execute(Stmt.Step))
            {
            }
        }
    }

    public override DataTable GetSchemaTable()
    {
        EnsureOpen();

        if (FieldCount == 0)
        {
            throw new InvalidOperationException(Resources.NoData);
        }

        DataTable schemaTable = new("SchemaTable");

        schemaTable.Columns.Add(SchemaTableColumn.ColumnName, typeof(string));
        schemaTable.Columns.Add(SchemaTableColumn.ColumnOrdinal, typeof(int));
        schemaTable.Columns.Add(SchemaTableColumn.DataType, typeof(Type));
        schemaTable.Columns.Add(SchemaDataTypeNameColumn, typeof(string));
        schemaTable.Columns.Add(SchemaTableColumn.AllowDBNull, typeof(bool));
        schemaTable.Columns.Add(SchemaTableColumn.ColumnSize, typeof(int));
        schemaTable.Columns.Add(SchemaIsKeyColumn, typeof(bool));
        schemaTable.Columns.Add(SchemaIsUniqueColumn, typeof(bool));
        schemaTable.Columns.Add(SchemaTableOptionalColumn.BaseCatalogName, typeof(string));
        schemaTable.Columns.Add(SchemaTableColumn.BaseTableName, typeof(string));
        schemaTable.Columns.Add(SchemaTableColumn.BaseColumnName, typeof(string));
        schemaTable.Columns.Add(SchemaTableColumn.IsAliased, typeof(bool));
        schemaTable.Columns.Add(SchemaTableColumn.IsExpression, typeof(bool));

        for (int ordinal = 0; ordinal < FieldCount; ordinal++)
        {
            string columnName = GetName(ordinal);
            string? baseColumnName = Stmt.GetColumnOriginName(ordinal);
            string? baseTableName = Stmt.GetColumnTableName(ordinal);
            string? baseCatalogName = Stmt.GetColumnDatabaseName(ordinal);

            bool hasOrigin = !string.IsNullOrEmpty(baseColumnName) && !string.IsNullOrEmpty(baseTableName);
            bool isAliased = !string.Equals(columnName, baseColumnName, StringComparison.Ordinal);

            DataRow row = schemaTable.NewRow();
            row[SchemaTableColumn.ColumnName] = columnName;
            row[SchemaTableColumn.ColumnOrdinal] = ordinal;
            row[SchemaTableColumn.DataType] = GetFieldType(ordinal);
            row[SchemaDataTypeNameColumn] = GetDataTypeName(ordinal);
            row[SchemaTableColumn.AllowDBNull] = true;
            row[SchemaTableColumn.ColumnSize] = DBNull.Value;
            row[SchemaIsKeyColumn] = DBNull.Value;
            row[SchemaIsUniqueColumn] = DBNull.Value;
            row[SchemaTableOptionalColumn.BaseCatalogName] = baseCatalogName ?? string.Empty;
            row[SchemaTableColumn.BaseTableName] = baseTableName ?? string.Empty;
            row[SchemaTableColumn.BaseColumnName] = baseColumnName ?? string.Empty;
            row[SchemaTableColumn.IsAliased] = isAliased;
            row[SchemaTableColumn.IsExpression] = !hasOrigin;

            schemaTable.Rows.Add(row);
        }

        return schemaTable;
    }

    public override bool Read()
    {
        EnsureOpen();
        _readStarted = true;
        if (_prefetched)
        {
            _prefetched = false;
        }
        else
        {
            _hasRow = _stmt is not null && _executionScope.Execute(Stmt.Step);
        }

        if (!_hasRow)
        {
            AddChangesFromCurrentStatement();
        }

        if (!_hasRow && _behavior.HasFlag(System.Data.CommandBehavior.SingleRow))
        {
            Close();
        }

        return _hasRow;
    }

    public override Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        EnsureOpen();
        _readStarted = true;
        if (_prefetched)
        {
            _prefetched = false;
        }
        else
        {
            _hasRow = _stmt is not null && _executionScope.Execute(Stmt.Step, cancellationToken);
        }

        if (!_hasRow)
        {
            AddChangesFromCurrentStatement();
        }

        if (!_hasRow && _behavior.HasFlag(System.Data.CommandBehavior.SingleRow))
        {
            Close();
        }

        return Task.FromResult(_hasRow);
    }

    public override void Close()
    {
        if (_closed) return;

        DrainRemainingStatements();

        _closed = true;
        _stmt?.Dispose();
        _executionScope.Dispose();
        _session.Gate.Release();
        if (_behavior.HasFlag(System.Data.CommandBehavior.CloseConnection))
        {
            _command.Connection?.Close();
        }
    }

    public override Task CloseAsync()
    {
        Close();
        return Task.CompletedTask;
    }

    public bool IsDBNull(string name) => IsDBNull(GetOrdinal(name));

    protected override void Dispose(bool disposing)
    {
        if (disposing) Close();
        base.Dispose(disposing);
    }

    private void EnsureOpen([CallerMemberName] string? operation = null)
    {
        if (_closed)
        {
            throw new InvalidOperationException(Resources.DataReaderClosed(operation ?? "operation"));
        }
    }

    private void EnsurePrefetched()
    {
        if (_prefetched || _readStarted)
            return;

        _hasRow = _stmt is not null && _executionScope.Execute(Stmt.Step);
        _prefetched = true;
    }

    private void EnsureHasRow([CallerMemberName] string? operation = null)
    {
        EnsureOpen(operation);

        if (!_readStarted || !_hasRow)
        {
            throw new InvalidOperationException(Resources.NoData);
        }
    }

    private void ValidateOrdinal(int ordinal)
    {
        if (ordinal < 0 || ordinal >= FieldCount)
        {
            throw new ArgumentOutOfRangeException(nameof(ordinal), ordinal, "Column ordinal is out of range.");
        }
    }

    private void AddChangesFromCurrentStatement()
    {
        if (_stmt is null)
        {
            return;
        }

        int changes = _session.Native.Changes();
        if (changes > 0)
        {
            _recordsAffected = _recordsAffected < 0 ? changes : _recordsAffected + changes;
        }
    }

    private void DrainRemainingStatements()
    {
        try
        {
            if (_stmt is not null)
            {
                while (_executionScope.Execute(Stmt.Step))
                {
                }

                AddChangesFromCurrentStatement();
            }

            while (true)
            {
                _stmt?.Dispose();
                _stmt = _executionScope.Execute(() => _command.PrepareAndBindNext(_session, _batchState));
                if (_stmt is null)
                {
                    break;
                }

                while (_executionScope.Execute(Stmt.Step))
                {
                }

                AddChangesFromCurrentStatement();
            }
        }
        catch
        {
            // Close/Dispose should not throw while draining trailing statements.
        }
    }

    private static DateTime JulianDayToDateTime(double julianDay)
    {
        const double unixEpochJulianDay = 2440587.5;
        double ticksSinceUnixEpoch = (julianDay - unixEpochJulianDay) * TimeSpan.TicksPerDay;
        long roundedMilliseconds = (long)Math.Round(ticksSinceUnixEpoch / TimeSpan.TicksPerMillisecond, MidpointRounding.AwayFromZero);
        DateTime utc = DateTime.UnixEpoch.AddMilliseconds(roundedMilliseconds);
        return DateTime.SpecifyKind(utc, DateTimeKind.Unspecified);
    }

    private Type InferFieldType(int ordinal)
        => Stmt.GetColumnType(ordinal) switch
        {
            1 => typeof(long),
            2 => typeof(double),
            3 => typeof(string),
            4 => typeof(byte[]),
            _ => GetFieldTypeFromDeclaration(ordinal),
        };

    private string GetDataTypeNameFromDeclaration(int ordinal)
    {
        string? declaredType = Stmt.GetColumnDeclType(ordinal);
        if (string.IsNullOrWhiteSpace(declaredType))
        {
            return "BLOB";
        }

        int parenIndex = declaredType.IndexOf('(');
        return parenIndex >= 0
            ? declaredType.Substring(0, parenIndex).Trim()
            : declaredType;
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
    private Type GetFieldTypeFromDeclaration(int ordinal)
    {
        string? declaredType = Stmt.GetColumnDeclType(ordinal);
        if (string.IsNullOrWhiteSpace(declaredType))
        {
            return typeof(byte[]);
        }

        string typeName = declaredType.Trim().ToUpperInvariant();
        if (typeName.Contains("INT", StringComparison.Ordinal))
        {
            return typeof(long);
        }

        if (typeName.Contains("CHAR", StringComparison.Ordinal)
            || typeName.Contains("CLOB", StringComparison.Ordinal)
            || typeName.Contains("TEXT", StringComparison.Ordinal)
            || typeName.Contains("NCHAR", StringComparison.Ordinal)
            || typeName.Contains("NVARCHAR", StringComparison.Ordinal))
        {
            return typeof(string);
        }

        if (typeName.Contains("REAL", StringComparison.Ordinal)
            || typeName.Contains("FLOA", StringComparison.Ordinal)
            || typeName.Contains("DOUB", StringComparison.Ordinal))
        {
            return typeof(double);
        }

        if (typeName.Contains("BLOB", StringComparison.Ordinal) || typeName.Length == 0)
        {
            return typeof(byte[]);
        }

        return typeof(string);
    }
}
