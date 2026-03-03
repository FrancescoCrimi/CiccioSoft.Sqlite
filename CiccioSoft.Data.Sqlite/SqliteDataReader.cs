using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using CiccioSoft.Sqlite.Interop;

namespace CiccioSoft.Data.Sqlite;

public class SqliteDataReader : DbDataReader
{
    private readonly SqliteCommand _command;
    private readonly SqliteSession _session;
    private readonly System.Data.CommandBehavior _behavior;
    private readonly Sqlite3Stmt _stmt;
    private readonly SqliteCommand.CommandExecutionScope _executionScope;
    private bool _hasRow;
    private bool _prefetched;
    private bool _readStarted;
    private bool _closed;

    private SqliteDataReader(
        SqliteCommand command,
        SqliteSession session,
        System.Data.CommandBehavior behavior,
        Sqlite3Stmt stmt,
        SqliteCommand.CommandExecutionScope executionScope)
    {
        _command = command;
        _session = session;
        _behavior = behavior;
        _stmt = stmt;
        _executionScope = executionScope;
    }

    internal static SqliteDataReader Create(SqliteCommand command, SqliteSession session, System.Data.CommandBehavior behavior)
    {
        session.Gate.Wait();
        try
        {
            SqliteCommand.CommandExecutionScope scope = command.CreateExecutionScope(session, CancellationToken.None);
            Sqlite3Stmt stmt;
            try
            {
                stmt = scope.Execute(() => command.PrepareAndBind(session));
            }
            catch
            {
                scope.Dispose();
                throw;
            }

            return new SqliteDataReader(command, session, behavior, stmt, scope);
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
            Sqlite3Stmt stmt;
            try
            {
                stmt = scope.Execute(() => command.PrepareAndBind(session));
            }
            catch
            {
                scope.Dispose();
                throw;
            }

            return new SqliteDataReader(command, session, behavior, stmt, scope);
        }
        catch
        {
            session.Gate.Release();
            throw;
        }
    }

    public override object this[int ordinal] => GetValue(ordinal);
    public override object this[string name] => GetValue(GetOrdinal(name));
    public override int Depth => 0;
    public override int FieldCount => _closed ? 0 : _stmt.ColumnCount();
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
        ReadOnlySpan<byte> blob = _stmt.GetBlob(ordinal);
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
        string? declaredType = _stmt.GetColumnDeclType(ordinal);
        if (!string.IsNullOrWhiteSpace(declaredType))
        {
            return declaredType;
        }

        return _stmt.GetColumnType(ordinal) switch
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
    public override double GetDouble(int ordinal) => _stmt.GetDouble(ordinal);
    public override IEnumerator GetEnumerator() => new DbEnumerator(this);

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
    public override Type GetFieldType(int ordinal)
    {
        return _stmt.GetColumnType(ordinal) switch
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
    public override int GetInt32(int ordinal) => _stmt.GetInt(ordinal);
    public override long GetInt64(int ordinal) => _stmt.GetLong(ordinal);
    public override string GetName(int ordinal) => _stmt.GetColumnName(ordinal) ?? string.Empty;

    public override int GetOrdinal(string name)
    {
        for (int i = 0; i < FieldCount; i++)
        {
            if (string.Equals(GetName(i), name, StringComparison.OrdinalIgnoreCase)) return i;
        }

        throw new IndexOutOfRangeException($"Column '{name}' not found.");
    }

    public override string GetString(int ordinal) => _stmt.GetString(ordinal) ?? string.Empty;

    public override object GetValue(int ordinal)
    {
        if (IsDBNull(ordinal)) return DBNull.Value;
        return _stmt.GetColumnType(ordinal) switch
        {
            1 => _stmt.GetLong(ordinal),
            2 => _stmt.GetDouble(ordinal),
            3 => _stmt.GetString(ordinal) ?? string.Empty,
            4 => _stmt.GetBlob(ordinal).ToArray(),
            _ => DBNull.Value,
        };
    }

    public override int GetValues(object[] values)
    {
        int count = Math.Min(values.Length, FieldCount);
        for (int i = 0; i < count; i++) values[i] = GetValue(i);
        return count;
    }


    public override bool IsDBNull(int ordinal) => _stmt.GetColumnType(ordinal) == 5;
    public override bool NextResult() => false;

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
        schemaTable.Columns.Add(SchemaTableColumn.DataTypeName, typeof(string));
        schemaTable.Columns.Add(SchemaTableColumn.AllowDBNull, typeof(bool));
        schemaTable.Columns.Add(SchemaTableOptionalColumn.IsKey, typeof(bool));
        schemaTable.Columns.Add(SchemaTableOptionalColumn.IsUnique, typeof(bool));
        schemaTable.Columns.Add(SchemaTableOptionalColumn.BaseCatalogName, typeof(string));
        schemaTable.Columns.Add(SchemaTableColumn.BaseTableName, typeof(string));
        schemaTable.Columns.Add(SchemaTableColumn.BaseColumnName, typeof(string));
        schemaTable.Columns.Add(SchemaTableColumn.IsAliased, typeof(bool));
        schemaTable.Columns.Add(SchemaTableColumn.IsExpression, typeof(bool));

        for (int ordinal = 0; ordinal < FieldCount; ordinal++)
        {
            string columnName = GetName(ordinal);
            string? baseColumnName = _stmt.GetColumnOriginName(ordinal);
            string? baseTableName = _stmt.GetColumnTableName(ordinal);
            string? baseCatalogName = _stmt.GetColumnDatabaseName(ordinal);

            bool hasOrigin = !string.IsNullOrEmpty(baseColumnName) && !string.IsNullOrEmpty(baseTableName);
            bool isAliased = !string.Equals(columnName, baseColumnName, StringComparison.Ordinal);

            DataRow row = schemaTable.NewRow();
            row[SchemaTableColumn.ColumnName] = columnName;
            row[SchemaTableColumn.ColumnOrdinal] = ordinal;
            row[SchemaTableColumn.DataType] = GetFieldTypeFromDeclaration(ordinal);
            row[SchemaTableColumn.DataTypeName] = GetDataTypeName(ordinal);
            row[SchemaTableColumn.AllowDBNull] = true;
            row[SchemaTableOptionalColumn.IsKey] = false;
            row[SchemaTableOptionalColumn.IsUnique] = false;
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
            _hasRow = _executionScope.Execute(_stmt.Step);
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

        return Task.Run(() =>
        {
            EnsureOpen();
            _readStarted = true;
            if (_prefetched)
            {
                _prefetched = false;
            }
            else
            {
                _hasRow = _executionScope.Execute(_stmt.Step, cancellationToken);
            }

            if (!_hasRow && _behavior.HasFlag(System.Data.CommandBehavior.SingleRow))
            {
                Close();
            }

            return _hasRow;
        }, cancellationToken);
    }

    public override void Close()
    {
        if (_closed) return;
        _closed = true;
        _stmt.Dispose();
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

        _hasRow = _executionScope.Execute(_stmt.Step);
        _prefetched = true;
    }
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
    private Type GetFieldTypeFromDeclaration(int ordinal)
    {
        string? declaredType = _stmt.GetColumnDeclType(ordinal);
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
