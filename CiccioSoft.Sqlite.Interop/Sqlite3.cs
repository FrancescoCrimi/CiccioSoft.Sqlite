// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CiccioSoft.Sqlite.Interop.Native;

namespace CiccioSoft.Sqlite.Interop;

public sealed unsafe class Sqlite3SafeHandle : SafeHandle
{
    internal Sqlite3SafeHandle(sqlite3* sqlite3)
        : base((nint)sqlite3, true)
    {
    }

    public override bool IsInvalid => handle == nint.Zero;

    public sqlite3* AsStructPointer() => (sqlite3*)handle;

    protected override bool ReleaseHandle()
    {
        return Sqlite3Native.sqlite3_close_v2((sqlite3*)handle) == Sqlite3Native.SQLITE_OK;
    }
}

/// <summary>
/// Provides a high-performance, low-allocation wrapper for a SQLite database connection.
/// </summary>
/// <threadsafety>
/// This class is not inherently thread-safe. Concurrent access to a single SQLite connection 
/// should be synchronized or managed according to SQLite's threading modes.
/// </threadsafety>
public sealed unsafe class Sqlite3 : IDisposable
{
    private readonly Sqlite3SafeHandle _handle;

    private Sqlite3(Sqlite3SafeHandle handle)
    {
        _handle = handle;
    }

    internal Sqlite3SafeHandle Handle => _handle;

    /// <summary>
    /// Opening A New Database Connection.
    /// </summary>
    /// <param name="filename">The path to the database file to be opened.</param>
    /// <returns>A new <see cref="Sqlite3"/> instance representing the database connection.</returns>
    /// <exception cref="SqliteInteropException">Thrown if the database cannot be opened.</exception>
    public static Sqlite3 Open(string filename)
    {
        return Open(filename, SqliteOpenFlags.ReadWrite | SqliteOpenFlags.Create);
    }

    /// <summary>
    /// Opening A New Database Connection with explicit <c>sqlite3_open_v2</c> flags.
    /// </summary>
    /// <param name="filename">The path (or URI) to the database file.</param>
    /// <param name="flags">The SQLite open flags (for example <c>SQLITE_OPEN_READWRITE | SQLITE_OPEN_CREATE</c>).</param>
    /// <param name="useUri">If true, <c>SQLITE_OPEN_URI</c> is enforced to allow URI filenames.</param>
    /// <param name="vfsName">Optional VFS module name. Use <c>null</c> to use SQLite default VFS.</param>
    /// <returns>A new <see cref="Sqlite3"/> connection.</returns>
    /// <exception cref="SqliteInteropException">Thrown if the database cannot be opened.</exception>
    public static Sqlite3 Open(string filename, SqliteOpenFlags flags, bool useUri = false, string? vfsName = null)
    {
        sqlite3* pDb = default;

        SqliteOpenFlags openFlags = useUri ? flags | SqliteOpenFlags.Uri : flags;

        using var filenameBuffer = new Utf8SafeStackBuffer(filename, stackalloc byte[512]);
        using var vfsBuffer = new Utf8SafeStackBuffer(vfsName, stackalloc byte[512]);

        fixed (byte* pFilename = filenameBuffer, pVfsRaw = vfsBuffer)
        {
            // SOLUZIONE DEL BUG: Se il parametro originale C# era nullo/vuoto, 
            // passiamo un puntatore 'null' effettivo a SQLite, altrimenti usiamo pVfsRaw.
            byte* pVfs = string.IsNullOrEmpty(vfsName) ? null : pVfsRaw;

            SqliteResult result = (SqliteResult)Sqlite3Native.sqlite3_open_v2(pFilename, &pDb, (int)openFlags, pVfs);
            Sqlite3SafeHandle sqlite3Handle = new Sqlite3SafeHandle(pDb);

            // Se l'apertura fallisce, Dobbiamo COMUNQUE recuperare l'errore 
            // PRIMA di chiudere l'handle, altrimenti pDb diventa invalido.
            if (result != SqliteResult.OK)
            {
                var exception = new SqliteInteropException(result, sqlite3Handle, "SQLite open");

                // IMPORTANTE: SQLite alloca memoria anche se open fallisce.
                // Dobbiamo chiudere pDb manualmente o tramite l'handle.
                sqlite3Handle.Dispose();

                throw exception;
            }

            // Se tutto è andato bene, incapsuliamo l'handle sicuro
            return new Sqlite3(sqlite3Handle);
        }
    }

    public void Execute(ReadOnlySpan<byte> sql)
    {
        ThrowIfInvalid();

        fixed (byte* pBuf = sql)
        {
            SqliteResult result = (SqliteResult)Sqlite3Native.sqlite3_exec(
                _handle.AsStructPointer(),
                pBuf,
                null,
                null,
                null);
            CheckResult(result);
        }
    }

    /// <summary>
    /// One-Step Query Execution Interface.
    /// </summary>
    /// <param name="sql">The SQL string to execute (e.g., 'CREATE TABLE', 'INSERT', 'VACUUM').</param>
    /// <exception cref="ObjectDisposedException">Thrown if the database connection is closed.</exception>
    /// <exception cref="SqliteInteropException">Thrown if SQLite returns an error during execution.</exception>
    public void Execute(string sql)
    {
        ThrowIfInvalid();

        using var utf8Buffer = new Utf8SafeStackBuffer(sql, stackalloc byte[1024]);
        Execute(utf8Buffer.AsSpan());
    }

    /// <summary>
    /// Compiling An SQL Statement.
    /// </summary>
    /// <param name="sql">The SQL query string to compile.</param>
    /// <returns>A new <see cref="Sqlite3Stmt"/> instance wrapping the compiled statement.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the database connection is no longer valid.</exception>
    /// <exception cref="SqliteInteropException">Thrown if the SQL syntax is invalid or the statement cannot be prepared.</exception>
    public Sqlite3Stmt Prepare(string sql)
    {
        return Prepare(sql, SqlitePrepareFlags.None);
    }

    /// <summary>
    /// Compiles an SQL statement using <c>sqlite3_prepare_v3</c>, enabling explicit prepare flags.
    /// </summary>
    /// <param name="sql">The SQL query string to compile.</param>
    /// <param name="prepareFlags">Flags such as <see cref="SqlitePrepareFlags.Persistent"/> or <see cref="SqlitePrepareFlags.NoVtab"/>.</param>
    /// <returns>A new <see cref="Sqlite3Stmt"/> instance wrapping the compiled statement.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the database connection is no longer valid.</exception>
    /// <exception cref="SqliteInteropException">Thrown if the SQL syntax is invalid or the statement cannot be prepared.</exception>
    public Sqlite3Stmt Prepare(string sql, SqlitePrepareFlags prepareFlags = SqlitePrepareFlags.None)
    {
        ThrowIfInvalid();

        using var utf8Buffer = new Utf8SafeStackBuffer(sql, stackalloc byte[1024]);

        fixed (byte* pBuf = utf8Buffer)
        {
            // Chiamata nativa
            sqlite3_stmt* pStmt = default;
            SqliteResult result = (SqliteResult)Sqlite3Native.sqlite3_prepare_v3(
                _handle.AsStructPointer(),
                pBuf,
                utf8Buffer.Length, // Lunghezza esatta dei dati
                (uint)prepareFlags,
                &pStmt,
                null);
            var stmtSafeHandle = new Sqlite3StmtSafeHandle(pStmt);

            if (result != SqliteResult.OK)
            {
                var exception = new SqliteInteropException(result, _handle, "SQLite prepare");
                stmtSafeHandle.Dispose();
                throw exception;
            }

            return new Sqlite3Stmt(stmtSafeHandle, _handle);
        }
    }

    /// <summary>
    /// Compiles the next SQL statement starting from a byte offset within a batch SQL text.
    /// </summary>
    /// <param name="sql">The full SQL batch text.</param>
    /// <param name="sqlByteOffset">The UTF-8 byte offset where statement preparation should start.</param>
    /// <param name="nextSqlByteOffset">The UTF-8 byte offset immediately after the prepared statement.</param>
    /// <param name="prepareFlags">Flags such as <see cref="SqlitePrepareFlags.Persistent"/> or <see cref="SqlitePrepareFlags.NoVtab"/>.</param>
    /// <returns>
    /// A prepared statement if one is found at the given offset; otherwise <c>null</c> when only whitespace/comments remain.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="sqlByteOffset"/> is outside the SQL byte buffer range.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the database connection is no longer valid.</exception>
    /// <exception cref="SqliteInteropException">Thrown if the statement cannot be prepared.</exception>
    public Sqlite3Stmt? Prepare(string sql, int sqlByteOffset, out int nextSqlByteOffset, SqlitePrepareFlags prepareFlags = SqlitePrepareFlags.None)
    {
        ThrowIfInvalid();

        using var utf8Buffer = new Utf8SafeStackBuffer(sql, stackalloc byte[1024]);
        int dataLength = utf8Buffer.Length + 1; // +1 per il null terminator

        if ((uint)sqlByteOffset > (uint)dataLength)
            throw new ArgumentOutOfRangeException(nameof(sqlByteOffset));

        fixed (byte* pBuf = utf8Buffer)
        {
            byte* pStart = pBuf + sqlByteOffset;
            int remainingLength = dataLength - sqlByteOffset;

            sqlite3_stmt* pStmt = default;
            byte* pTail = null;
            SqliteResult result = (SqliteResult)Sqlite3Native.sqlite3_prepare_v3(
                _handle.AsStructPointer(),
                pStart,
                remainingLength,
                (uint)prepareFlags,
                &pStmt,
                &pTail);
            var stmtSafeHandle = new Sqlite3StmtSafeHandle(pStmt);

            if (result != SqliteResult.OK)
            {
                var exception = new SqliteInteropException(result, _handle, "SQLite prepare");
                stmtSafeHandle.Dispose();
                throw exception;
            }

            int consumedBytes = pTail is null ? remainingLength : (int)(pTail - pStart);
            nextSqlByteOffset = sqlByteOffset + consumedBytes;

            // Todo: fixa qui e fixa in SqliteCommand PrepareAndBindNext e PrepareAndEnumerateStatements
            if ((nint)pStmt == nint.Zero)
            {
                return null;
            }

            return new Sqlite3Stmt(stmtSafeHandle, _handle);
        }
    }

    /// <summary>
    /// Returns the row ID of the last successful INSERT into the database from this connection.
    /// </summary>
    /// <returns>The 64-bit row identifier of the last inserted row.</returns>
    public long LastInsertRowId()
    {
        ThrowIfInvalid();
        return Sqlite3Native.sqlite3_last_insert_rowid(_handle.AsStructPointer());
    }

    /// <summary>
    /// Returns the number of rows modified, inserted, or deleted by the last finished SQL statement.
    /// </summary>
    /// <returns>The number of affected rows.</returns>
    //TODO: check int/long return
    public int Changes()
    {
        ThrowIfInvalid();
        return Sqlite3Native.sqlite3_changes(_handle.AsStructPointer());
    }

    /// <summary>
    /// Returns the total number of rows modified, inserted, or deleted since this connection was opened.
    /// </summary>
    public long TotalChanges()
    {
        ThrowIfInvalid();
        return Sqlite3Native.sqlite3_total_changes64(_handle.AsStructPointer());
    }

    /// <summary>
    /// Returns <c>true</c> if the connection is currently in auto-commit mode.
    /// </summary>
    public bool GetAutoCommit()
    {
        ThrowIfInvalid();
        return Sqlite3Native.sqlite3_get_autocommit(_handle.AsStructPointer()) != 0;
    }

    /// <summary>
    /// Queries or changes a runtime limit for the connection. 
    /// Pass -1 to read the current limit, or a positive value to lower it.
    /// </summary>
    /// <param name="id">The category of the limit to check or modify.</param>
    /// <param name="newVal">The new limit value, or -1 to only query the current limit.</param>
    /// <returns>The limit value that was in effect before this call.</returns>
    public int Limit(SqliteLimitCategory id, int newVal)
    {
        ThrowIfInvalid();
        return Sqlite3Native.sqlite3_limit(_handle.AsStructPointer(), (int)id, newVal);
    }

    /// <summary>
    /// Gets the current transaction state for a specific schema, or the highest state across all schemas if null.
    /// </summary>
    /// <param name="schemaName">The name of the schema (e.g., "main"). Pass null for the global connection state.</param>
    /// <returns>The specific transaction state.</returns>
    /// <exception cref="SqliteInteropException">Thrown if the schema name is invalid.</exception>
    public SqliteTransactionState TransactionState(string? schemaName = null)
    {
        ThrowIfInvalid();

        int result;

        if (schemaName is null)
        {
            result = Sqlite3Native.sqlite3_txn_state(_handle.AsStructPointer(), null);
        }
		else
		{
			using var utf8Buffer = new Utf8SafeStackBuffer(schemaName, stackalloc byte[512]);
			fixed (byte* pSchema = utf8Buffer)
			{
				result = Sqlite3Native.sqlite3_txn_state(_handle.AsStructPointer(), pSchema);
			}
		}

        // Se il risultato è -1, lo schema specificato non esiste
        if (result == -1)
        {
            throw new SqliteInteropException(
                $"[SQLite Error in {GetType().Name}.TransactionState] The schema '{schemaName}' is not a valid attached database.");
        }

        return (SqliteTransactionState)result;
    }

    /// <summary>
    /// Determines whether a attached database is read-only.
    /// </summary>
    /// <param name="databaseName">The name of the database (e.g., "main", "temp").</param>
    /// <returns>True if the database is read-only; false if it is read/write.</returns>
    /// <exception cref="SqliteInteropException">Thrown if the database name is not found on this connection.</exception>
    public bool DbReadOnly(string databaseName = "main")
    {
        ThrowIfInvalid();

        using var utf8Buffer = new Utf8SafeStackBuffer(databaseName, stackalloc byte[512]);

        fixed (byte* pSchema = utf8Buffer)
        {
            int result = Sqlite3Native.sqlite3_db_readonly(_handle.AsStructPointer(), pSchema);
            return result switch
            {
                1 => true,  // Read-Only
                0 => false, // Read-Write
                _ => throw new SqliteInteropException(
                    $"The database '{databaseName}' is not attached to this connection.")
            };
        }
    }

    /// <summary>
    /// Returns the latest extended SQLite error code for this connection.
    /// </summary>
    public SqliteExtendedErrorCode ExtendedErrCode()
    {
        ThrowIfInvalid();
        return (SqliteExtendedErrorCode)Sqlite3Native.sqlite3_extended_errcode(_handle.AsStructPointer());
    }

    /// <summary>
    /// Returns the byte offset in SQL text where the latest parse error was detected.
    /// </summary>
    /// <returns>The zero-based offset, or -1 if unavailable.</returns>
    public int GetLastErrorOffset()
    {
        ThrowIfInvalid();
        return Sqlite3Native.sqlite3_error_offset(_handle.AsStructPointer());
    }

    /// <summary>
    /// Sets a busy timeout on this connection.
    /// </summary>
    /// <param name="milliseconds">The timeout in milliseconds.</param>
    public void BusyTimeout(int milliseconds)
    {
        ThrowIfInvalid();
        var result = (SqliteResult)Sqlite3Native.sqlite3_busy_timeout(_handle.AsStructPointer(), milliseconds);
        if (result == SqliteResult.OK)
            return;
        CheckResult(result);
    }

    /// <summary>
    /// Enables or disables extended result codes for this connection.
    /// </summary>
    /// <param name="enabled">True to enable extended result codes.</param>
    public void ExtendedResultCodes(bool enabled)
    {
        ThrowIfInvalid();
        var result = (SqliteResult)Sqlite3Native.sqlite3_extended_result_codes(_handle.AsStructPointer(), enabled ? 1 : 0);
        if (result == SqliteResult.OK)
            return;
        CheckResult(result);
    }

    /// <summary>
    /// Interrupts any pending operation running on this connection.
    /// </summary>
    public void Interrupt()
    {
        ThrowIfInvalid();
        Sqlite3Native.sqlite3_interrupt(_handle.AsStructPointer());
    }

    /// <summary>
    /// Returns the SQLite library version string used by the native runtime.
    /// </summary>
    /// <returns>
    /// A version string in the form <c>major.minor.patch</c> (for example, <c>3.46.0</c>).
    /// </returns>
    public static string? LibVersion()
    {
        byte* pLibVersion = Sqlite3Native.sqlite3_libversion();
        return Marshal.PtrToStringUTF8((nint)pLibVersion);
    }

    /// <summary>
    /// Returns the SQLite library version number used by the native runtime.
    /// </summary>
    /// <returns>
    /// An integer representation of the version in the format <c>MMmmpp</c> (major, minor, patch).
    /// </returns>
    public static int LibVersionNumber()
    {
        return Sqlite3Native.sqlite3_libversion_number();
    }

    /// <summary>
    /// Retrieves metadata information about a specific column in a table.
    /// </summary>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="columnName">The name of the column.</param>
    /// <param name="dataType">Output: The declared data type of the column (e.g., "TEXT", "INTEGER", "REAL", "BLOB").</param>
    /// <param name="collSeq">Output: The collating sequence (e.g., "BINARY", "NOCASE", "RTRIM").</param>
    /// <param name="isNotNull">Output: Whether the column has a NOT NULL constraint.</param>
    /// <param name="isPrimaryKey">Output: Whether the column is part of the primary key.</param>
    /// <param name="isAutoIncrement">Output: Whether the column has the AUTOINCREMENT keyword.</param>
    /// <remarks>
    /// <para>
    /// This method provides type-safe access to SQLite's table_column_metadata function.
    /// It leverages zero-allocation marshalling techniques to minimize heap pressure.
    /// </para>
    /// <para>
    /// The metadata is retrieved from the "main" database attachment by default.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if tableName or columnName is null.</exception>
    /// <exception cref="SqliteInteropException">Thrown if the metadata cannot be retrieved.</exception>
    public void GetTableColumnMetadata(
        string tableName,
        string columnName,
        out string? dataType,
        out string? collSeq,
        out bool isNotNull,
        out bool isPrimaryKey,
        out bool isAutoIncrement)
    {
        ThrowIfInvalid();
        ArgumentNullException.ThrowIfNull(tableName);
        ArgumentNullException.ThrowIfNull(columnName);

        if (tableName.Length == 0)
            throw new ArgumentException("Table name cannot be empty.", nameof(tableName));
        if (columnName.Length == 0)
            throw new ArgumentException("Column name cannot be empty.", nameof(columnName));

        byte* pDataType = null;
        byte* pCollSeq = null;
        int notNull = 0;
        int primaryKey = 0;
        int autoInc = 0;

        const int smallStringThreshold = 256;
        int tableNameByteCount = Encoding.UTF8.GetByteCount(tableName) + 1;
        int columnNameByteCount = Encoding.UTF8.GetByteCount(columnName) + 1;
        int totalNeeded = tableNameByteCount + columnNameByteCount;

        byte[]? pooled = null;
        Span<byte> combinedBuffer = totalNeeded <= smallStringThreshold * 2
            ? stackalloc byte[totalNeeded]
            : (pooled = ArrayPool<byte>.Shared.Rent(totalNeeded)).AsSpan(0, totalNeeded);

        try
        {
            Span<byte> tableNameBuffer = combinedBuffer[..tableNameByteCount];
            Span<byte> columnNameBuffer = combinedBuffer.Slice(tableNameByteCount, columnNameByteCount);

            Encoding.UTF8.GetBytes(tableName, tableNameBuffer);
            tableNameBuffer[^1] = 0;

            Encoding.UTF8.GetBytes(columnName, columnNameBuffer);
            columnNameBuffer[^1] = 0;

            sqlite3* dbHandle = _handle.AsStructPointer();

            fixed (byte* pTableName = tableNameBuffer)
            fixed (byte* pColumnName = columnNameBuffer)
            {
                SqliteResult rc = (SqliteResult)Sqlite3Native.sqlite3_table_column_metadata(
                    dbHandle,
                    null,
                    pTableName,
                    pColumnName,
                    &pDataType,
                    &pCollSeq,
                    &notNull,
                    &primaryKey,
                    &autoInc);

                if (rc != SqliteResult.OK)
                {
                    string operation = $"SQLite metadata lookup for column '{columnName}' in table '{tableName}'";
                    throw new SqliteInteropException(rc, _handle, operation);
                }
            }

            dataType = pDataType != null ? Marshal.PtrToStringUTF8((nint)pDataType) : null;
            collSeq = pCollSeq != null ? Marshal.PtrToStringUTF8((nint)pCollSeq) : null;
            isNotNull = notNull != 0;
            isPrimaryKey = primaryKey != 0;
            isAutoIncrement = autoInc != 0;
        }
        finally
        {
            if (pooled != null)
                ArrayPool<byte>.Shared.Return(pooled);
        }
    }


    #region Private Methods

    private void ThrowIfInvalid()
    {
        if (_handle.IsInvalid) throw new ObjectDisposedException(nameof(Sqlite3));
    }

    private void CheckResult(SqliteResult res, [CallerMemberName] string caller = "")
    {
        if (res == SqliteResult.OK)
            return;
        throw new SqliteInteropException(res, _handle, $"SQLite error in: {GetType().Name}.{caller}");
    }

    #endregion

    public void Dispose() => _handle.Dispose();
}
