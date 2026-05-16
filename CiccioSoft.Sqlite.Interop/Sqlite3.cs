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

namespace CiccioSoft.Sqlite.Interop;

public sealed class Sqlite3Handle : SafeHandleZeroOrMinusOneIsInvalid
{
    // public Sqlite3Handle() : base(true) { }
    internal Sqlite3Handle(nint handle) : base(true)
    {
        SetHandle(handle);
    }
    protected override bool ReleaseHandle()
    {
        // return SqliteNative.sqlite3_close_v2(handle) == SqliteNative.SQLITE_OK;
        Sqlite3Native.sqlite3_close_v2(handle);
        return true;
    }
}

/// <summary>
/// Provides a high-performance, low-allocation wrapper for a SQLite database connection.
/// </summary>
/// <remarks>
/// <b>Design Principles:</b>
/// <list type="bullet">
/// <item>
/// <description>Zero-Allocation Marshalling: Extensively uses <c>stackalloc</c> and <see cref="System.Buffers.ArrayPool{T}"/> to minimize Managed Heap churn during string-to-UTF8 conversions.</description>
/// </item>
/// <item>
/// <description>Native Interoperability: Optimized for <c>P/Invoke</c> using <c>unsafe</c> code and <c>Span&lt;T&gt;</c> for direct memory access.</description>
/// </item>
/// <item>
/// <description>Resource Safety: Implements <see cref="IDisposable"/> using a <see cref="SafeHandle"/> pattern to ensure deterministic release of native SQLite resources.</description>
/// </item>
/// </list>
/// </remarks>
/// <threadsafety>
/// This class is not inherently thread-safe. Concurrent access to a single SQLite connection 
/// should be synchronized or managed according to SQLite's threading modes.
/// </threadsafety>
public sealed unsafe class Sqlite3 : IDisposable
{
    private readonly Sqlite3Handle _handle;

    internal Sqlite3(Sqlite3Handle handle)
    {
        _handle = handle;
    }

    /// <summary>
    /// Opening A New Database Connection.
    /// </summary>
    /// <param name="filename">The path to the database file to be opened.</param>
    /// <returns>A new <see cref="Sqlite3"/> instance representing the database connection.</returns>
    /// <remarks>
    /// <b>Implementation Details:</b>
    /// <list type="bullet">
    /// <item>
    /// <description>Hybrid allocation: Uses <c>stackalloc</c> for paths up to 1KB, falling back to <see cref="ArrayPool{T}"/> for longer paths to avoid Managed Heap churn.</description>
    /// </item>
    /// <item>
    /// <description>Zero-copy string marshalling: Encodes the filename directly into a temporary buffer with a manual null terminator, bypassing redundant <see cref="string"/> allocations.</description>
    /// </item>
    /// <item>
    /// <description>Safe Error Handling: Captures the error message via <c>sqlite3_errmsg</c> <b>before</b> closing the pointer, ensuring the error description remains valid for the exception.</description>
    /// </item>
    /// <item>
    /// <description>Resource Leak Prevention: Explicitly calls <c>sqlite3_close_v2</c> even if the open operation fails, as SQLite may allocate resources for the handle during a failed attempt.</description>
    /// </item>
    /// </list>
    /// </remarks>
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
        nint pDb = default;

        SqliteOpenFlags openFlags = useUri ? flags | SqliteOpenFlags.Uri : flags;

        int filenameDataLength = Encoding.UTF8.GetByteCount(filename);
        int filenameTotalNeeded = filenameDataLength + 1;

        int vfsDataLength = vfsName is null ? 0 : Encoding.UTF8.GetByteCount(vfsName);
        int vfsTotalNeeded = vfsDataLength + 1;

        byte[]? arrayFromPool = null;

        int totalNeeded = filenameTotalNeeded + (vfsName is null ? 0 : vfsTotalNeeded);
        Span<byte> buffer = totalNeeded <= 1024
            ? stackalloc byte[totalNeeded]
            : (arrayFromPool = ArrayPool<byte>.Shared.Rent(totalNeeded)).AsSpan(0, totalNeeded);

        try
        {
            Span<byte> filenameBuffer = buffer[..filenameTotalNeeded];
            Encoding.UTF8.GetBytes(filename, filenameBuffer);
            filenameBuffer[filenameDataLength] = 0;

            if (vfsName is not null)
            {
                Span<byte> vfsBuffer = buffer.Slice(filenameTotalNeeded, vfsTotalNeeded);
                Encoding.UTF8.GetBytes(vfsName, vfsBuffer);
                vfsBuffer[vfsDataLength] = 0;
            }

            fixed (byte* pBuffer = buffer)
            {
                byte* pFilename = pBuffer;
                byte* pVfs = vfsName is null ? null : pBuffer + filenameTotalNeeded;
                SqliteResult result = (SqliteResult)Sqlite3Native.sqlite3_open_v2(pFilename, &pDb, (int)openFlags, pVfs);

                // Se l'apertura fallisce, Dobbiamo COMUNQUE recuperare l'errore 
                // PRIMA di chiudere l'handle, altrimenti pDb diventa invalido.
                if (result != SqliteResult.OK)
                {
                    SqliteInteropException exception = SqliteErrorHelper.CreateException(result, pDb, "SQLite open");

                    // IMPORTANTE: SQLite alloca memoria anche se open fallisce.
                    // Dobbiamo chiudere pDb manualmente o tramite l'handle.
                    if (pDb != nint.Zero)
                    {
                        Sqlite3Native.sqlite3_close_v2(pDb);
                    }

                    throw exception;
                }

                // Se tutto è andato bene, incapsuliamo l'handle sicuro
                return new Sqlite3(new Sqlite3Handle(pDb));
            }
        }
        finally
        {
            if (arrayFromPool != null)
                ArrayPool<byte>.Shared.Return(arrayFromPool);
        }
    }

    /// <summary>
    /// One-Step Query Execution Interface.
    /// </summary>
    /// <param name="sql">The SQL string to execute (e.g., 'CREATE TABLE', 'INSERT', 'VACUUM').</param>
    /// <remarks>
    /// Zero-Allocation Optimization Strategy:
    /// <list type="bullet">
    /// <item>
    /// <description>Uses <c>stackalloc</c> for queries smaller than 1KB to avoid Managed Heap allocation.</description>
    /// </item>
    /// <item>
    /// <description>Falls back to <see cref="System.Buffers.ArrayPool{T}"/> for larger queries to minimize Garbage Collector pressure.</description>
    /// </item>
    /// <item>
    /// <description>Manually appends the null terminator required by <c>sqlite3_exec</c> to prevent unnecessary string concatenations.</description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the database connection is closed.</exception>
    /// <exception cref="SqliteInteropException">Thrown if SQLite returns an error during execution.</exception>
    public void Execute(string sql)
    {
        ThrowIfInvalid();

        int dataLength = Encoding.UTF8.GetByteCount(sql);
        int totalNeeded = dataLength + 1;

        byte[]? arrayFromPool = null;
        Span<byte> buffer = totalNeeded <= 1024
            ? stackalloc byte[totalNeeded]
            : (arrayFromPool = ArrayPool<byte>.Shared.Rent(totalNeeded)).AsSpan(0, totalNeeded);

        try
        {
            Encoding.UTF8.GetBytes(sql, buffer);
            buffer[dataLength] = 0;

            fixed (byte* pBuf = buffer)
            {
                SqliteResult result = (SqliteResult)Sqlite3Native.sqlite3_exec(
                    _handle.DangerousGetHandle(),
                    pBuf,
                    null,
                    null,
                    null);
                SqliteErrorHelper.ThrowOnError(result, _handle.DangerousGetHandle(), "SQLite exec");
            }
        }
        finally
        {
            if (arrayFromPool != null)
                ArrayPool<byte>.Shared.Return(arrayFromPool);
        }
    }

    /// <summary>
    /// Compiling An SQL Statement.
    /// </summary>
    /// <param name="sql">The SQL query string to compile.</param>
    /// <returns>A new <see cref="Sqlite3Stmt"/> instance wrapping the compiled statement.</returns>
    /// <remarks>
    /// <b>Performance Optimizations:</b>
    /// <list type="bullet">
    /// <item>
    /// <description>Hybrid allocation: Uses <c>stackalloc</c> for queries up to 1KB, falling back to <see cref="ArrayPool{T}"/> for larger SQL strings.</description>
    /// </item>
    /// <item>
    /// <description>Explicit Length: Passes the exact UTF-8 byte count to <c>sqlite3_prepare_v2</c>, allowing SQLite to bypass the internal null-terminator scan for better performance.</description>
    /// </item>
    /// <item>
    /// <description>Safe Cleanup: If preparation fails but an internal statement pointer is partially allocated, it is immediately finalized to prevent native memory leaks.</description>
    /// </item>
    /// </list>
    /// </remarks>
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

        int dataLength = Encoding.UTF8.GetByteCount(sql);
        byte[]? arrayFromPool = null;
        Span<byte> buffer = dataLength <= 1024
            ? stackalloc byte[dataLength]
            : (arrayFromPool = ArrayPool<byte>.Shared.Rent(dataLength)).AsSpan(0, dataLength);

        try
        {
            Encoding.UTF8.GetBytes(sql, buffer);

            fixed (byte* pBuf = buffer)
            {
                // Chiamata nativa
                nint pStmt = default;
                SqliteResult result = (SqliteResult)Sqlite3Native.sqlite3_prepare_v3(
                    _handle.DangerousGetHandle(),
                    pBuf,
                    dataLength, // Lunghezza esatta dei dati
                    (uint)prepareFlags,
                    &pStmt,
                    null);

                if (result != SqliteResult.OK)
                {
                    SqliteInteropException exception = SqliteErrorHelper.CreateException(result, _handle.DangerousGetHandle(), "SQLite prepare");

                    // Se pStmt è stato allocato nonostante l'errore, va chiuso.
                    if (pStmt != nint.Zero)
                        Sqlite3Native.sqlite3_finalize(pStmt);

                    throw exception;
                }

                return new Sqlite3Stmt(new Sqlite3StmtHandle(pStmt));
            }
        }
        finally
        {
            if (arrayFromPool != null)
                ArrayPool<byte>.Shared.Return(arrayFromPool);
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

        int dataLength = Encoding.UTF8.GetByteCount(sql);
        if ((uint)sqlByteOffset > (uint)dataLength)
        {
            throw new ArgumentOutOfRangeException(nameof(sqlByteOffset));
        }

        byte[]? arrayFromPool = null;
        Span<byte> buffer = dataLength <= 1024
            ? stackalloc byte[dataLength]
            : (arrayFromPool = ArrayPool<byte>.Shared.Rent(dataLength)).AsSpan(0, dataLength);

        try
        {
            Encoding.UTF8.GetBytes(sql, buffer);

            fixed (byte* pBuf = buffer)
            {
                byte* pStart = pBuf + sqlByteOffset;
                int remainingLength = dataLength - sqlByteOffset;

                nint pStmt = default;
                byte* pTail = null;
                SqliteResult result = (SqliteResult)Sqlite3Native.sqlite3_prepare_v3(
                    _handle.DangerousGetHandle(),
                    pStart,
                    remainingLength,
                    (uint)prepareFlags,
                    &pStmt,
                    &pTail);

                if (result != SqliteResult.OK)
                {
                    SqliteInteropException exception = SqliteErrorHelper.CreateException(result, _handle.DangerousGetHandle(), "SQLite prepare");
                    if (pStmt != nint.Zero)
                        Sqlite3Native.sqlite3_finalize(pStmt);

                    throw exception;
                }

                int consumedBytes = pTail is null ? remainingLength : (int)(pTail - pStart);
                nextSqlByteOffset = sqlByteOffset + consumedBytes;

                if (pStmt == nint.Zero)
                {
                    return null;
                }

                return new Sqlite3Stmt(new Sqlite3StmtHandle(pStmt));
            }
        }
        finally
        {
            if (arrayFromPool != null)
                ArrayPool<byte>.Shared.Return(arrayFromPool);
        }
    }

    /// <summary>
    /// Returns the row ID of the last successful INSERT into the database from this connection.
    /// </summary>
    /// <returns>The 64-bit row identifier of the last inserted row.</returns>
    public long LastInsertRowId()
    {
        ThrowIfInvalid();
        return Sqlite3Native.sqlite3_last_insert_rowid(_handle.DangerousGetHandle());
    }

    /// <summary>
    /// Returns the number of rows modified, inserted, or deleted by the last finished SQL statement.
    /// </summary>
    /// <returns>The number of affected rows.</returns>
    public int Changes()
    {
        ThrowIfInvalid();
        return Sqlite3Native.sqlite3_changes(_handle.DangerousGetHandle());
    }

    /// <summary>
    /// Returns the total number of rows modified, inserted, or deleted since this connection was opened.
    /// </summary>
    public long TotalChanges()
    {
        ThrowIfInvalid();
        return Sqlite3Native.sqlite3_total_changes64(_handle.DangerousGetHandle());
    }

    /// <summary>
    /// Returns <c>true</c> if the connection is currently in auto-commit mode.
    /// </summary>
    public bool IsAutoCommit()
    {
        ThrowIfInvalid();
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
        ThrowIfInvalid();
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
        ThrowIfInvalid();

        if (schemaName is null)
        {
            return Sqlite3Native.sqlite3_txn_state(_handle.DangerousGetHandle(), null);
        }

        int dataLength = Encoding.UTF8.GetByteCount(schemaName);
        int totalNeeded = dataLength + 1;

        byte[]? arrayFromPool = null;
        Span<byte> buffer = totalNeeded <= 256
            ? stackalloc byte[totalNeeded]
            : (arrayFromPool = ArrayPool<byte>.Shared.Rent(totalNeeded)).AsSpan(0, totalNeeded);

        try
        {
            Encoding.UTF8.GetBytes(schemaName, buffer);
            buffer[dataLength] = 0;

            fixed (byte* pSchema = buffer)
            {
                return Sqlite3Native.sqlite3_txn_state(_handle.DangerousGetHandle(), pSchema);
            }
        }
        finally
        {
            if (arrayFromPool != null)
                ArrayPool<byte>.Shared.Return(arrayFromPool);
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
        ThrowIfInvalid();

        int dataLength = Encoding.UTF8.GetByteCount(schemaName);
        int totalNeeded = dataLength + 1;

        byte[]? arrayFromPool = null;
        Span<byte> buffer = totalNeeded <= 256
            ? stackalloc byte[totalNeeded]
            : (arrayFromPool = ArrayPool<byte>.Shared.Rent(totalNeeded)).AsSpan(0, totalNeeded);

        try
        {
            Encoding.UTF8.GetBytes(schemaName, buffer);
            buffer[dataLength] = 0;

            fixed (byte* pSchema = buffer)
            {
                int result = Sqlite3Native.sqlite3_db_readonly(_handle.DangerousGetHandle(), pSchema);
                if (result < 0)
                {
                    SqliteErrorHelper.ThrowOnError(SqliteResult.Error, _handle.DangerousGetHandle(), "SQLite db readonly");
                }

                return result != 0;
            }
        }
        finally
        {
            if (arrayFromPool != null)
                ArrayPool<byte>.Shared.Return(arrayFromPool);
        }
    }

    /// <summary>
    /// Returns the latest extended SQLite error code for this connection.
    /// </summary>
    public SqliteExtendedErrorCode GetLastExtendedErrorCode()
    {
        ThrowIfInvalid();
        return (SqliteExtendedErrorCode)Sqlite3Native.sqlite3_extended_errcode(_handle.DangerousGetHandle());
    }

    /// <summary>
    /// Returns the byte offset in SQL text where the latest parse error was detected.
    /// </summary>
    /// <returns>The zero-based offset, or -1 if unavailable.</returns>
    public int GetLastErrorOffset()
    {
        ThrowIfInvalid();
        return Sqlite3Native.sqlite3_error_offset(_handle.DangerousGetHandle());
    }

    /// <summary>
    /// Sets a busy timeout on this connection.
    /// </summary>
    /// <param name="milliseconds">The timeout in milliseconds.</param>
    public void SetBusyTimeout(int milliseconds)
    {
        ThrowIfInvalid();
        SqliteErrorHelper.ThrowOnError(
            (SqliteResult)Sqlite3Native.sqlite3_busy_timeout(_handle.DangerousGetHandle(), milliseconds),
            _handle.DangerousGetHandle(),
            "SQLite busy timeout");
    }

    /// <summary>
    /// Enables or disables extended result codes for this connection.
    /// </summary>
    /// <param name="enabled">True to enable extended result codes.</param>
    public void SetExtendedResultCodes(bool enabled)
    {
        ThrowIfInvalid();
        SqliteErrorHelper.ThrowOnError(
            (SqliteResult)Sqlite3Native.sqlite3_extended_result_codes(_handle.DangerousGetHandle(), enabled ? 1 : 0),
            _handle.DangerousGetHandle(),
            "SQLite extended result codes");
    }

    /// <summary>
    /// Interrupts any pending operation running on this connection.
    /// </summary>
    public void Interrupt()
    {
        ThrowIfInvalid();
        Sqlite3Native.sqlite3_interrupt(_handle.DangerousGetHandle());
    }

    /// <summary>
    /// Returns the SQLite library version string used by the native runtime.
    /// </summary>
    /// <returns>
    /// A version string in the form <c>major.minor.patch</c> (for example, <c>3.46.0</c>).
    /// </returns>
    public string LibVersion()
    {
        byte* pLibVersion = Sqlite3Native.sqlite3_libversion();
        return Marshal.PtrToStringUTF8((nint)pLibVersion)!;
    }

    /// <summary>
    /// Returns the SQLite library version number used by the native runtime.
    /// </summary>
    /// <returns>
    /// An integer representation of the version in the format <c>MMmmpp</c> (major, minor, patch).
    /// </returns>
    public int LibVersionNumber()
    {
        return Sqlite3Native.sqlite3_libversion_number();
    }

    public Sqlite3Backup InitBackup(Sqlite3 source)
    {
        return InitBackup("main", source, "main");
    }

    public Sqlite3Backup InitBackup(string destinationDatabaseName, Sqlite3 source, string sourceDatabaseName = "main")
    {
        ThrowIfInvalid();
        ArgumentNullException.ThrowIfNull(source);
        source.ThrowIfInvalid();
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDatabaseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDatabaseName);

        int destinationBytes = Encoding.UTF8.GetByteCount(destinationDatabaseName);
        int sourceBytes = Encoding.UTF8.GetByteCount(sourceDatabaseName);
        int destinationTotalNeeded = destinationBytes + 1;
        int sourceTotalNeeded = sourceBytes + 1;
        int totalNeeded = destinationTotalNeeded + sourceTotalNeeded;

        byte[]? pooled = null;
        Span<byte> buffer = totalNeeded <= 256
            ? stackalloc byte[totalNeeded]
            : (pooled = ArrayPool<byte>.Shared.Rent(totalNeeded)).AsSpan(0, totalNeeded);

        try
        {
            Span<byte> destinationBuffer = buffer[..destinationTotalNeeded];
            Span<byte> sourceBuffer = buffer.Slice(destinationTotalNeeded, sourceTotalNeeded);

            Encoding.UTF8.GetBytes(destinationDatabaseName, destinationBuffer);
            destinationBuffer[destinationBytes] = 0;

            Encoding.UTF8.GetBytes(sourceDatabaseName, sourceBuffer);
            sourceBuffer[sourceBytes] = 0;

            fixed (byte* pDest = destinationBuffer)
            fixed (byte* pSource = sourceBuffer)
            {
                nint destinationHandle = _handle.DangerousGetHandle();
                nint sourceHandle = source._handle.DangerousGetHandle();

                nint backupHandle = Sqlite3Native.sqlite3_backup_init(
                    destinationHandle,
                    pDest,
                    sourceHandle,
                    pSource);

                if (backupHandle == nint.Zero)
                {
                    throw SqliteErrorHelper.CreateException(
                        (SqliteResult)Sqlite3Native.sqlite3_errcode(destinationHandle),
                        destinationHandle,
                        "SQLite backup init");
                }

                return new Sqlite3Backup(new Sqlite3BackupHandle(backupHandle));
            }
        }
        finally
        {
            if (pooled != null)
                ArrayPool<byte>.Shared.Return(pooled);
        }
    }


    private void ThrowIfInvalid()
    {
        if (_handle.IsInvalid) throw new ObjectDisposedException(nameof(Sqlite3));
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
                    throw SqliteErrorHelper.CreateException(rc, dbHandle, operation);
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
