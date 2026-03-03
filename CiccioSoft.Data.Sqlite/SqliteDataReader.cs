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
    public override int FieldCount => _closed ? 0 : _stmt?.ColumnCount() ?? 0;
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
    public override int RecordsAffected => -1;

    public override bool GetBoolean(int ordinal) => GetInt32(ordinal) != 0;
    public override byte GetByte(int ordinal) => (byte)GetInt32(ordinal);

    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
    {
        ReadOnlySpan<byte> blob = Stmt.GetBlob(ordinal);
        int available = Math.Max(0, blob.Length - (int)dataOffset);
        int toCopy = Math.Min(length, available);
        if (buffer is not null && toCopy > 0)
        {
            blob.Slice((int)dataOffset, toCopy).CopyTo(buffer.AsSpan(bufferOffset, toCopy));
        }

        return available;
    }

    public override char GetChar(int ordinal) => GetString(ordinal)[0];

    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
    {
        string s = GetString(ordinal);
        int available = Math.Max(0, s.Length - (int)dataOffset);
        int toCopy = Math.Min(length, available);
        if (buffer is not null && toCopy > 0)
        {
            s.AsSpan((int)dataOffset, toCopy).CopyTo(buffer.AsSpan(bufferOffset, toCopy));
        }

        return available;
    }

    public override string GetDataTypeName(int ordinal)
    {
        string? declaredType = Stmt.GetColumnDeclType(ordinal);
        if (!string.IsNullOrWhiteSpace(declaredType))
        {
            return declaredType;
        }

        return Stmt.GetColumnType(ordinal) switch
        {
            1 => "INTEGER",
            2 => "REAL",
            3 => "TEXT",
            4 => "BLOB",
            _ => "BLOB",
        };
    }

    public override DateTime GetDateTime(int ordinal) => DateTime.Parse(GetString(ordinal), System.Globalization.CultureInfo.InvariantCulture);
    public override decimal GetDecimal(int ordinal) => Convert.ToDecimal(GetValue(ordinal), System.Globalization.CultureInfo.InvariantCulture);
    public override double GetDouble(int ordinal) => Stmt.GetDouble(ordinal);
    public override IEnumerator GetEnumerator() => new DbEnumerator(this);

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
    public override Type GetFieldType(int ordinal)
    {
        return Stmt.GetColumnType(ordinal) switch
        {
            1 => typeof(long),
            2 => typeof(double),
            3 => typeof(string),
            4 => typeof(byte[]),
            _ => typeof(DBNull),
        };
    }

    public override float GetFloat(int ordinal) => (float)GetDouble(ordinal);
    public override Guid GetGuid(int ordinal) => Guid.Parse(GetString(ordinal));
    public override short GetInt16(int ordinal) => (short)GetInt32(ordinal);
    public override int GetInt32(int ordinal) => Stmt.GetInt(ordinal);
    public override long GetInt64(int ordinal) => Stmt.GetLong(ordinal);
    public override string GetName(int ordinal) => Stmt.GetColumnName(ordinal) ?? string.Empty;

    public override int GetOrdinal(string name)
    {
        for (int i = 0; i < FieldCount; i++)
        {
            if (string.Equals(GetName(i), name, StringComparison.OrdinalIgnoreCase)) return i;
        }

        throw new IndexOutOfRangeException($"Column '{name}' not found.");
    }

    public override string GetString(int ordinal) => Stmt.GetString(ordinal) ?? string.Empty;

    public override object GetValue(int ordinal)
    {
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
        int count = Math.Min(values.Length, FieldCount);
        for (int i = 0; i < count; i++) values[i] = GetValue(i);
        return count;
    }


    public override bool IsDBNull(int ordinal) => Stmt.GetColumnType(ordinal) == 5;
    public override bool NextResult()
    {
        EnsureOpen();

        if (_behavior.HasFlag(System.Data.CommandBehavior.SingleResult))
        {
            return false;
        }

        while (true)
        {
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
            throw new InvalidOperationException("The reader does not have result columns.");
        }

        DataTable schemaTable = new("SchemaTable");

        schemaTable.Columns.Add(SchemaTableColumn.ColumnName, typeof(string));
        schemaTable.Columns.Add(SchemaTableColumn.ColumnOrdinal, typeof(int));
        schemaTable.Columns.Add(SchemaTableColumn.DataType, typeof(Type));
        schemaTable.Columns.Add(SchemaDataTypeNameColumn, typeof(string));
        schemaTable.Columns.Add(SchemaTableColumn.AllowDBNull, typeof(bool));
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
            row[SchemaTableColumn.DataType] = GetFieldTypeFromDeclaration(ordinal);
            row[SchemaDataTypeNameColumn] = GetDataTypeName(ordinal);
            row[SchemaTableColumn.AllowDBNull] = true;
            row[SchemaIsKeyColumn] = false;
            row[SchemaIsUniqueColumn] = false;
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

        if (!_hasRow && _behavior.HasFlag(System.Data.CommandBehavior.SingleRow))
        {
            Close();
        }

        return Task.FromResult(_hasRow);
    }

    public override void Close()
    {
        if (_closed) return;
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

    private void EnsureOpen()
    {
        if (_closed) throw new InvalidOperationException("Reader is closed.");
    }

    private void EnsurePrefetched()
    {
        if (_prefetched || _readStarted)
            return;

        _hasRow = _stmt is not null && _executionScope.Execute(Stmt.Step);
        _prefetched = true;
    }
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
    private Type GetFieldTypeFromDeclaration(int ordinal)
    {
        string? declaredType = Stmt.GetColumnDeclType(ordinal);
        if (string.IsNullOrWhiteSpace(declaredType))
        {
            return GetFieldType(ordinal);
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
