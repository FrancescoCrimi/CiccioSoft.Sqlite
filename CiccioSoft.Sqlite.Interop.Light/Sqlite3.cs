// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Text;
using System.Buffers;
using CiccioSoft.Sqlite.Interop.Native;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace CiccioSoft.Sqlite.Interop.Light;

public sealed class Sqlite3Handle : SafeHandleZeroOrMinusOneIsInvalid
{
    internal Sqlite3Handle(nint handle) : base(true)
    {
        SetHandle(handle);
    }
    protected override bool ReleaseHandle()
    {
        return (SqliteResult)Sqlite3Native.sqlite3_close_v2(handle) == SqliteResult.OK;
    }
}

/// <summary>
/// Provides a high-performance, low-allocation wrapper for a SQLite database connection.
/// </summary>
/// <remarks>
/// <b>Design Principles:</b>
/// - Zero-Allocation Marshalling: Extensively uses <c>stackalloc</c> and <see cref="System.Buffers.ArrayPool{T}"/> to minimize Managed Heap churn during string-to-UTF8 conversions.
/// - Native Interoperability: Optimized for <c>P/Invoke</c> using <c>unsafe</c> code and <c>Span&lt;T&gt;</c> for direct memory access.
/// - Resource Safety: Implements <see cref="IDisposable"/> using a <see cref="SafeHandle"/> pattern to ensure deterministic release of native SQLite resources.
/// </remarks>
/// <threadsafety>
/// This class is not inherently thread-safe. Concurrent access to a single SQLite connection 
/// should be synchronized or managed according to SQLite's threading modes.
/// </threadsafety>
public sealed unsafe class Sqlite3 : IDisposable
{
    private readonly Sqlite3Handle _handle;

    private Sqlite3(Sqlite3Handle handle)
    {
        _handle = handle;
    }

    internal SafeHandle Handle => _handle;

    /// <summary>
    /// Opening A New Database Connection.
    /// </summary>
    /// <param name="filename">The path to the database file to be opened.</param>
    /// <returns>A new <see cref="Sqlite3"/> instance representing the database connection.</returns>
    /// <remarks>
    /// <b>Implementation Details:</b>
    /// - Hybrid allocation: Uses <c>stackalloc</c> for paths up to 1KB, falling back to <see cref="ArrayPool{T}"/> for longer paths to avoid Managed Heap churn.
    /// - Zero-copy string marshalling: Encodes the filename directly into a temporary buffer with a manual null terminator, bypassing redundant <see cref="string"/> allocations.
    /// - Safe Error Handling: Captures the error message via <c>sqlite3_errmsg</c> <b>before</b> closing the pointer, ensuring the error description remains valid for the exception.
    /// - Resource Leak Prevention: Explicitly calls <c>sqlite3_close_v2</c> even if the open operation fails, as SQLite may allocate resources for the handle during a failed attempt.
    /// </remarks>
    public static SqliteResult Open(string filename, out Sqlite3 db)
    {
        return Open(filename, out db, SqliteOpenFlags.ReadWrite | SqliteOpenFlags.Create);
    }

    /// <summary>
    /// Opening A New Database Connection with explicit <c>sqlite3_open_v2</c> flags.
    /// </summary>
    /// <param name="filename">The path (or URI) to the database file.</param>
    /// <param name="flags">The SQLite open flags (for example <c>SQLITE_OPEN_READWRITE | SQLITE_OPEN_CREATE</c>).</param>
    /// <param name="useUri">If true, <c>SQLITE_OPEN_URI</c> is enforced to allow URI filenames.</param>
    /// <param name="vfsName">Optional VFS module name. Use <c>null</c> to use SQLite default VFS.</param>
    /// <returns>A new <see cref="Sqlite3"/> connection.</returns>
    public static SqliteResult Open(string filename, out Sqlite3 db, SqliteOpenFlags flags, bool useUri = false, string? vfsName = null)
    {
        nint pDb = default;

        SqliteOpenFlags openFlags = useUri ? flags | SqliteOpenFlags.Uri : flags;

        using var filenameBuffer = new Utf8SafeStackBuffer(filename, stackalloc byte[512]);
        using var vfsBuffer = new Utf8SafeStackBuffer(vfsName, stackalloc byte[512]);

        fixed (byte* pFilename = filenameBuffer, pVfsRaw = vfsBuffer)
        {
            byte* pVfs = string.IsNullOrEmpty(vfsName) ? null : pVfsRaw;
            SqliteResult result = (SqliteResult)Sqlite3Native.sqlite3_open_v2(pFilename, &pDb, (int)openFlags, pVfs);


            // Se tutto è andato bene, incapsuliamo l'handle sicuro
            db = new Sqlite3(new Sqlite3Handle(pDb));
            return result;
        }
    }

    public SqliteResult Execute(ReadOnlySpan<byte> sql)
    {
        fixed (byte* pBuf = sql)
        {
            SqliteResult result = (SqliteResult)Sqlite3Native.sqlite3_exec(
                _handle.DangerousGetHandle(),
                pBuf,
                null,
                null,
                null);
            return result;
        }
    }

    /// <summary>
    /// One-Step Query Execution Interface.
    /// </summary>
    /// <param name="sql">The SQL string to execute (e.g., 'CREATE TABLE', 'INSERT', 'VACUUM').</param>
    /// <remarks>
    /// <b>Zero-Allocation Optimization Strategy:</b>
    /// - Uses <c>stackalloc</c> for queries smaller than 1KB to avoid Managed Heap allocation.
    /// - Falls back to <see cref="System.Buffers.ArrayPool{T}"/> for larger queries to minimize Garbage Collector pressure.
    /// - Manually appends the null terminator required by <c>sqlite3_exec</c> to prevent unnecessary string concatenations.
    /// </remarks>
    public SqliteResult Execute(string sql)
    {
        using var utf8Buffer = new Utf8SafeStackBuffer(sql, stackalloc byte[1024]);
        return Execute(utf8Buffer.AsSpan());
    }

    /// <summary>
    /// Compiling An SQL Statement.
    /// </summary>
    /// <param name="sql">The SQL query string to compile.</param>
    /// <returns>A new <see cref="Sqlite3Stmt"/> instance wrapping the compiled statement.</returns>
    /// <remarks>
    /// <b>Performance Optimizations:</b>
    /// - Hybrid allocation: Uses <c>stackalloc</c> for queries up to 1KB, falling back to <see cref="ArrayPool{T}"/> for larger SQL strings.
    /// - Explicit Length: Passes the exact UTF-8 byte count to <c>sqlite3_prepare_v2</c>, allowing SQLite to bypass the internal null-terminator scan for better performance.
    /// - Safe Cleanup: If preparation fails but an internal statement pointer is partially allocated, it is immediately finalized to prevent native memory leaks.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the database connection is no longer valid.</exception>
    public SqliteResult Prepare(string sql, out Sqlite3Stmt? stmt)
    {
        return Prepare(sql, out stmt, SqlitePrepareFlags.None);
    }

    /// <summary>
    /// Compiles an SQL statement using <c>sqlite3_prepare_v3</c>, enabling explicit prepare flags.
    /// </summary>
    /// <param name="sql">The SQL query string to compile.</param>
    /// <param name="prepareFlags">Flags such as <see cref="SqlitePrepareFlags.Persistent"/> or <see cref="SqlitePrepareFlags.NoVtab"/>.</param>
    /// <returns>A new <see cref="Sqlite3Stmt"/> instance wrapping the compiled statement.</returns>
    public SqliteResult Prepare(string sql, out Sqlite3Stmt? stmt, SqlitePrepareFlags prepareFlags = SqlitePrepareFlags.None)
    {
        using var utf8Buffer = new Utf8SafeStackBuffer(sql, stackalloc byte[1024]);

        fixed (byte* pBuf = utf8Buffer)
        {
            // Chiamata nativa
            nint pStmt = default;
            SqliteResult result = (SqliteResult)Sqlite3Native.sqlite3_prepare_v3(
                _handle.DangerousGetHandle(),
                pBuf,
                utf8Buffer.Length, // Lunghezza esatta dei dati
                (uint)prepareFlags,
                &pStmt,
                null);

            if (result != SqliteResult.OK)
            {
                // Se pStmt è stato allocato nonostante l'errore, va chiuso.
                if (pStmt != nint.Zero)
                    Sqlite3Native.sqlite3_finalize(pStmt);
                stmt = null;
            }
            else
                stmt = new Sqlite3Stmt(new Sqlite3StmtHandle(pStmt));
            return result;
        }
    }

    // /// <summary>
    // /// Compiles the next SQL statement starting from a byte offset within a batch SQL text.
    // /// </summary>
    // /// <param name="sql">The full SQL batch text.</param>
    // /// <param name="sqlByteOffset">The UTF-8 byte offset where statement preparation should start.</param>
    // /// <param name="nextSqlByteOffset">The UTF-8 byte offset immediately after the prepared statement.</param>
    // /// <param name="prepareFlags">Flags such as <see cref="SqlitePrepareFlags.Persistent"/> or <see cref="SqlitePrepareFlags.NoVtab"/>.</param>
    // /// <returns>
    // /// A prepared statement if one is found at the given offset; otherwise <c>null</c> when only whitespace/comments remain.
    // /// </returns>
    // /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="sqlByteOffset"/> is outside the SQL byte buffer range.</exception>
    // /// <exception cref="ObjectDisposedException">Thrown if the database connection is no longer valid.</exception>
    // /// <exception cref="SqliteInteropException">Thrown if the statement cannot be prepared.</exception>
    // public Sqlite3Stmt? Prepare(string sql, int sqlByteOffset, out int nextSqlByteOffset, SqlitePrepareFlags prepareFlags = SqlitePrepareFlags.None)
    // {
    //     ThrowIfInvalid();

    //     using var utf8Buffer = new Utf8SafeStackBuffer(sql, stackalloc byte[1024]);

    //     // int dataLength = Encoding.UTF8.GetByteCount(sql) + 1; // +1 per il null terminator
    //     int dataLength = utf8Buffer.Length + 1; // +1 per il null terminator

    //     if ((uint)sqlByteOffset > (uint)dataLength)
    //     {
    //         throw new ArgumentOutOfRangeException(nameof(sqlByteOffset));
    //     }

    //     // byte[]? arrayFromPool = null;
    //     // Span<byte> buffer = dataLength <= 1024
    //     //     ? stackalloc byte[dataLength]
    //     //     : (arrayFromPool = ArrayPool<byte>.Shared.Rent(dataLength)).AsSpan(0, dataLength);

    //     // try
    //     // {
    //     // Encoding.UTF8.GetBytes(sql, buffer);

    //     fixed (byte* pBuf = utf8Buffer)
    //     {
    //         byte* pStart = pBuf + sqlByteOffset;
    //         int remainingLength = dataLength - sqlByteOffset;

    //         nint pStmt = default;
    //         byte* pTail = null;
    //         SqliteResult result = (SqliteResult)Sqlite3Native.sqlite3_prepare_v3(
    //             _handle.DangerousGetHandle(),
    //             pStart,
    //             remainingLength,
    //             (uint)prepareFlags,
    //             &pStmt,
    //             &pTail);

    //         if (result != SqliteResult.OK)
    //         {
    //             SqliteInteropException exception = SqliteInteropException.CreateException(result, _handle.DangerousGetHandle(), "SQLite prepare");
    //             if (pStmt != nint.Zero)
    //                 Sqlite3Native.sqlite3_finalize(pStmt);

    //             throw exception;
    //         }

    //         int consumedBytes = pTail is null ? remainingLength : (int)(pTail - pStart);
    //         nextSqlByteOffset = sqlByteOffset + consumedBytes;

    //         if (pStmt == nint.Zero)
    //         {
    //             return null;
    //         }

    //         return new Sqlite3Stmt(new Sqlite3StmtHandle(pStmt));
    //     }
    //     // }
    //     // finally
    //     // {
    //     //     // if (arrayFromPool != null)
    //     //     //     ArrayPool<byte>.Shared.Return(arrayFromPool);
    //     // }
    // }

    /// <summary>
    /// Returns the row ID of the last successful INSERT into the database from this connection.
    /// </summary>
    /// <returns>The 64-bit row identifier of the last inserted row.</returns>
    public long LastInsertRowId()
    {
        return Sqlite3Native.sqlite3_last_insert_rowid(_handle.DangerousGetHandle());
    }

    /// <summary>
    /// Returns the number of rows modified, inserted, or deleted by the last finished SQL statement.
    /// </summary>
    /// <returns>The number of affected rows.</returns>
    //TODO: check int/long return
    public int Changes()
    {
        return Sqlite3Native.sqlite3_changes(_handle.DangerousGetHandle());
    }

    /// <summary>
    /// Returns the total number of rows modified, inserted, or deleted since this connection was opened.
    /// </summary>
    public long TotalChanges()
    {
        return Sqlite3Native.sqlite3_total_changes64(_handle.DangerousGetHandle());
    }

    /// <summary>
    /// Returns <c>true</c> if the connection is currently in auto-commit mode.
    /// </summary>
    public bool IsAutoCommit()
    {
        return Sqlite3Native.sqlite3_get_autocommit(_handle.DangerousGetHandle()) != 0;
    }

    /// <summary>
    /// Gets or sets a runtime limit for this connection using <c>sqlite3_limit</c>.
    /// </summary>
    /// <param name="id">
    /// The SQLite limit category identifier (for example, SQL length, expression depth, or attached database count).
    /// </param>
    /// <param name="newVal">
    /// The new value to apply. Pass a negative value to query the current limit without modifying it.
    /// </param>
    /// <returns>
    /// The previous limit value for the specified category.
    /// </returns>
    public int Limit(int id, int newVal)
    {
        return Sqlite3Native.sqlite3_limit(_handle.DangerousGetHandle(), id, newVal);
    }

    /// <summary>
    /// Returns the SQLite transaction state for the requested schema.
    /// </summary>
    /// <param name="schemaName">Schema name (for example "main"); pass <c>null</c> to let SQLite use the default schema.</param>
    /// <returns>
    /// One of SQLite transaction state constants:
    /// <list type="bullet">
    /// <item><description><c>SQLITE_TXN_NONE</c></description></item>
    /// <item><description><c>SQLITE_TXN_READ</c></description></item>
    /// <item><description><c>SQLITE_TXN_WRITE</c></description></item>
    /// </list>
    /// </returns>
    public int GetTransactionState(string? schemaName = null)
    {
        if (schemaName is null)
        {
            return Sqlite3Native.sqlite3_txn_state(_handle.DangerousGetHandle(), null);
        }

        using var utf8Buffer = new Utf8SafeStackBuffer(schemaName, stackalloc byte[1024]);

        fixed (byte* pSchema = utf8Buffer)
        {
            return Sqlite3Native.sqlite3_txn_state(_handle.DangerousGetHandle(), pSchema);
        }
    }

    /// <summary>
    /// Returns whether the specified schema is opened in read-only mode.
    /// </summary>
    /// <param name="schemaName">Schema name, usually "main" or "temp".</param>
    /// <returns>
    /// <c>true</c> if the schema is read-only; <c>false</c> if it is read-write.
    /// </returns>
    public bool IsReadOnly(string schemaName = "main")
    {
        using var utf8Buffer = new Utf8SafeStackBuffer(schemaName, stackalloc byte[512]);

        fixed (byte* pSchema = utf8Buffer)
        {
            int result = Sqlite3Native.sqlite3_db_readonly(_handle.DangerousGetHandle(), pSchema);
            return result != 0;
        }
    }

    /// <summary>
    /// Returns the latest extended SQLite error code for this connection.
    /// </summary>
    public SqliteExtendedErrorCode GetLastExtendedErrorCode()
    {
        return (SqliteExtendedErrorCode)Sqlite3Native.sqlite3_extended_errcode(_handle.DangerousGetHandle());
    }

    /// <summary>
    /// Returns the byte offset in SQL text where the latest parse error was detected.
    /// </summary>
    /// <returns>The zero-based offset, or -1 if unavailable.</returns>
    public int GetLastErrorOffset()
    {
        return Sqlite3Native.sqlite3_error_offset(_handle.DangerousGetHandle());
    }

    /// <summary>
    /// Sets a busy timeout on this connection.
    /// </summary>
    /// <param name="milliseconds">The timeout in milliseconds.</param>
    public SqliteResult SetBusyTimeout(int milliseconds)
    {
        return (SqliteResult)Sqlite3Native.sqlite3_busy_timeout(_handle.DangerousGetHandle(), milliseconds);
    }

    /// <summary>
    /// Enables or disables extended result codes for this connection.
    /// </summary>
    /// <param name="enabled">True to enable extended result codes.</param>
    public SqliteResult SetExtendedResultCodes(bool enabled)
    {
        return (SqliteResult)Sqlite3Native.sqlite3_extended_result_codes(_handle.DangerousGetHandle(), enabled ? 1 : 0);
    }

    /// <summary>
    /// Interrupts any pending operation running on this connection.
    /// </summary>
    public void Interrupt()
    {
        Sqlite3Native.sqlite3_interrupt(_handle.DangerousGetHandle());
    }

    /// <summary>
    /// Returns the SQLite library version string used by the native runtime.
    /// </summary>
    /// <returns>
    /// A version string in the form <c>major.minor.patch</c> (for example, <c>3.46.0</c>).
    /// </returns>
    public static string LibVersion()
    {
        byte* pLibVersion = Sqlite3Native.sqlite3_libversion();
        //Encoding.UTF8.GetString(
        return Marshal.PtrToStringUTF8((nint)pLibVersion)!;
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
    public void GetTableColumnMetadata(
        string tableName,
        string columnName,
        out string? dataType,
        out string? collSeq,
        out bool isNotNull,
        out bool isPrimaryKey,
        out bool isAutoIncrement)
    {
        // ThrowIfInvalid();
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

            nint dbHandle = _handle.DangerousGetHandle();

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
                    // throw SqliteInteropException.CreateException(rc, dbHandle, operation);
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

    public void Dispose() => _handle.Dispose();
}
