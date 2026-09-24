// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CiccioSoft.Sqlite.Native.Interop;

namespace CiccioSoft.Sqlite.Native;

/// <summary>
/// Provides a high-performance, low-allocation wrapper for a SQLite database connection.
/// </summary>
/// <threadsafety>
/// This class is not inherently thread-safe. Concurrent access to a single SQLite connection 
/// should be synchronized or managed according to SQLite's threading modes.
/// </threadsafety>
public sealed unsafe class Connection : SafeHandle
{

    #region Ctor and SafeHandle

    private Connection(sqlite3* sqlite3)
        : base((nint)sqlite3, true)
    {
    }

    public override bool IsInvalid => handle == nint.Zero;

    protected override bool ReleaseHandle()
    {
        _ = NativeMethods.sqlite3_close_v2((sqlite3*)handle);
        return true;
    }

    private sqlite3* Sqlite3Handle => (sqlite3*)DangerousGetHandle();

    #endregion


    #region Open

    /// <summary>
    /// Opening A New Database Connection with explicit <c>sqlite3_open_v2</c> flags.
    /// </summary>
    /// <param name="filename">The path (or URI) to the database file.</param>
    /// <param name="flags">The SQLite open flags (for example <c>SQLITE_OPEN_READWRITE | SQLITE_OPEN_CREATE</c>).</param>
    /// <param name="vfs">Optional VFS module name. Use <c>null</c> to use SQLite default VFS.</param>
    /// <returns>A new <see cref="Connection"/> connection.</returns>
    /// <exception cref="Exception">Thrown if the database cannot be opened.</exception>
    public static Connection Open(string filename, OpenFlags flags, string? vfs = null)
    {
        ArgumentNullException.ThrowIfNull(filename);

        if (filename.IndexOfAny(Path.GetInvalidPathChars()) != -1)
            throw new ArgumentException(
                "The path contains characters that are invalid for the current operating system.",
                nameof(filename));

        vfs = vfs is "" ? null : vfs;

        using var filenameBuffer = new Utf8CStringBuffer(filename, stackalloc byte[512]);
        using var vfsBuffer = new Utf8CStringBuffer(vfs!, stackalloc byte[512]);

        return Open(filenameBuffer.AsSpan(), flags, vfsBuffer.AsSpan());
    }

    public static Connection Open(ReadOnlySpan<byte> filename, OpenFlags flags, ReadOnlySpan<byte> vfs)
    {
        flags |= OpenFlags.Uri;
        flags |= OpenFlags.Exrescode;

        fixed (byte* pFilename = filename, pVfs = vfs)
        {
            sqlite3* psqlite3 = default;
            var result = (ResultCode)NativeMethods.sqlite3_open_v2(
                pFilename,
                &psqlite3,
                (int)flags,
                pVfs);
            var connection = new Connection(psqlite3);

            if (result != ResultCode.OK)
            {
                var exception = Exception.ReturnException(
                    result,
                    connection.ErrorMessage(),
                    $"{nameof(Connection)}.{nameof(Open)}");

                connection.Dispose();
                throw exception;
            }

            return connection;
        }
    }

    #endregion


    #region Execute

    /// <summary>
    /// One-Step Query Execution Interface.
    /// </summary>
    /// <param name="sql">The SQL string to execute (e.g., 'CREATE TABLE', 'INSERT', 'VACUUM').</param>
    /// <exception cref="ObjectDisposedException">Thrown if the database connection is closed.</exception>
    /// <exception cref="Exception">Thrown if SQLite returns an error during execution.</exception>
    public void Execute(string sql)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        using var utf8Buffer = new Utf8CStringBuffer(sql, stackalloc byte[1024]);
        ExecuteCore(utf8Buffer.AsSpan());
    }

    public void Execute(ReadOnlySpan<byte> sql)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ExecuteCore(sql);
    }

    private void ExecuteCore(ReadOnlySpan<byte> sql)
    {
        fixed (byte* pBuf = sql)
        {
            int result = NativeMethods.sqlite3_exec(
                 Sqlite3Handle,
                 pBuf,
                 null,
                 null,
                 null);
            if ((ResultCode)result == ResultCode.OK)
                return;
            else
                ThrowException((ResultCode)result, ErrorMessage());
        }
    }

    #endregion


    #region Prepare

    /// <summary>
    /// Compiles an SQL statement using <c>sqlite3_prepare_v3</c>, enabling explicit prepare flags.
    /// </summary>
    /// <param name="sql">The SQL query string to compile.</param>
    /// <param name="prepareFlags">Flags such as <see cref="PrepareFlags.Persistent"/> or <see cref="PrepareFlags.NoVtab"/>.</param>
    /// <returns>A new <see cref="Statement"/> instance wrapping the compiled statement.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the database connection is no longer valid.</exception>
    /// <exception cref="Exception">Thrown if the SQL syntax is invalid or the statement cannot be prepared.</exception>
    public Statement Prepare(string sql, PrepareFlags prepareFlags = PrepareFlags.None)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        using var utf8Buffer = new Utf8CStringBuffer(sql, stackalloc byte[1024]);
        return PrepareCore(utf8Buffer.AsSpan(), prepareFlags);
    }

    public Statement Prepare(ReadOnlySpan<byte> sql, PrepareFlags prepareFlags = PrepareFlags.None)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        return PrepareCore(sql, prepareFlags);
    }

    public Statement PrepareCore(ReadOnlySpan<byte> sql, PrepareFlags prepareFlags = PrepareFlags.None)
    {
        fixed (byte* pBuf = sql)
        {
            sqlite3_stmt* pStmt = default;

            ResultCode result = (ResultCode)NativeMethods.sqlite3_prepare_v3(
                 Sqlite3Handle,
                 pBuf,
                 sql.Length,                  // Lunghezza esatta dei dati
                 (uint)prepareFlags,
                 &pStmt,
                 null);

            var statement = new Statement(pStmt, this);

            if (result != ResultCode.OK)
            {
                statement.Dispose();
                ThrowException(result, ErrorMessage());
            }

            return statement;
        }
    }

    /// <summary>
    /// Compiles the next SQL statement starting from a byte offset within a batch SQL text.
    /// </summary>
    /// <param name="sql">The full SQL batch text.</param>
    /// <param name="sqlByteOffset">The UTF-8 byte offset where statement preparation should start.</param>
    /// <param name="nextSqlByteOffset">The UTF-8 byte offset immediately after the prepared statement.</param>
    /// <param name="prepareFlags">Flags such as <see cref="PrepareFlags.Persistent"/> or <see cref="PrepareFlags.NoVtab"/>.</param>
    /// <returns>
    /// A prepared statement if one is found at the given offset; otherwise <c>null</c> when only whitespace/comments remain.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="sqlByteOffset"/> is outside the SQL byte buffer range.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the database connection is no longer valid.</exception>
    /// <exception cref="Exception">Thrown if the statement cannot be prepared.</exception>
    public Statement? Prepare(string sql, int sqlByteOffset, out int nextSqlByteOffset, PrepareFlags prepareFlags = PrepareFlags.None)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        using var utf8Buffer = new Utf8CStringBuffer(sql, stackalloc byte[1024]);
        int dataLength = utf8Buffer.Length + 1; // +1 per il null terminator

        if ((uint)sqlByteOffset > (uint)dataLength)
            throw new ArgumentOutOfRangeException(nameof(sqlByteOffset));

        fixed (byte* pBuf = utf8Buffer)
        {
            byte* pStart = pBuf + sqlByteOffset;
            int remainingLength = dataLength - sqlByteOffset;

            sqlite3_stmt* pStmt = default;
            byte* pTail = null;

            ResultCode result = (ResultCode)NativeMethods.sqlite3_prepare_v3(
                   Sqlite3Handle,
                   pStart,
                   remainingLength,
                   (uint)prepareFlags,
                   &pStmt,
                   &pTail);

            var statement = new Statement(pStmt, this);

            if (result != ResultCode.OK)
            {
                statement.Dispose();
                ThrowException(result, ErrorMessage());
            }

            int consumedBytes = pTail is null ? remainingLength : (int)(pTail - pStart);
            nextSqlByteOffset = sqlByteOffset + consumedBytes;

            // Todo: fixa qui e fixa in SqliteCommand PrepareAndBindNext e PrepareAndEnumerateStatements
            if ((nint)pStmt == nint.Zero)
            {
                return null;
            }

            return statement;
        }
    }

    #endregion


    #region Other Connection Method

    /// <summary>
    /// Returns the row ID of the last successful INSERT into the database from this connection.
    /// </summary>
    /// <returns>The 64-bit row identifier of the last inserted row.</returns>
    public long LastInsertRowId()
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        long rtn = NativeMethods.sqlite3_last_insert_rowid(Sqlite3Handle);
        return rtn;
    }

    /// <summary>
    /// Returns the number of rows modified, inserted, or deleted by the last finished SQL statement.
    /// </summary>
    /// <returns>The number of affected rows.</returns>
    //TODO: check int/long return
    public int Changes()
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        int rtn = NativeMethods.sqlite3_changes(Sqlite3Handle);
        return rtn;
    }

    /// <summary>
    /// Returns the total number of rows modified, inserted, or deleted since this connection was opened.
    /// </summary>
    public long TotalChanges()
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        long rtn = NativeMethods.sqlite3_total_changes64(Sqlite3Handle);
        return rtn;
    }

    /// <summary>
    /// Returns <c>true</c> if the connection is currently in auto-commit mode.
    /// </summary>
    public bool GetAutoCommit()
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        int rtn = NativeMethods.sqlite3_get_autocommit(Sqlite3Handle);
        return rtn != 0;
    }

    /// <summary>
    /// Queries or changes a runtime limit for the connection. 
    /// Pass -1 to read the current limit, or a positive value to lower it.
    /// </summary>
    /// <param name="id">The category of the limit to check or modify.</param>
    /// <param name="newVal">The new limit value, or -1 to only query the current limit.</param>
    /// <returns>The limit value that was in effect before this call.</returns>
    public int Limit(LimitCategory id, int newVal)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        int rtn = NativeMethods.sqlite3_limit(Sqlite3Handle, (int)id, newVal);
        return rtn;
    }

    /// <summary>
    /// Gets the current transaction state for a specific schema, or the highest state across all schemas if null.
    /// </summary>
    /// <param name="schemaName">The name of the schema (e.g., "main"). Pass null for the global connection state.</param>
    /// <returns>The specific transaction state.</returns>
    /// <exception cref="Exception">Thrown if the schema name is invalid.</exception>
    public TransactionState TransactionState(string? schemaName = null)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        int result;

        // if (schemaName is null)
        // {
        //     result = NativeMethods.sqlite3_txn_state(Sqlite3Handle, null);
        //     GC.KeepAlive(_handle);
        // }

        // else
        // {

        using var utf8Buffer = new Utf8CStringBuffer(schemaName!, stackalloc byte[512]);
        fixed (byte* pSchema = utf8Buffer)
        {
            result = NativeMethods.sqlite3_txn_state(Sqlite3Handle, pSchema);
        }

        // Se il risultato è -1, lo schema specificato non esiste
        if (result == -1)
        {
            throw new ArgumentException(
                $"The schema '{schemaName}' is not a valid attached database.");
        }
        // }

        return (TransactionState)result;
    }

    /// <summary>
    /// Determines whether a attached database is read-only.
    /// </summary>
    /// <param name="databaseName">The name of the database (e.g., "main", "temp").</param>
    /// <returns>True if the database is read-only; false if it is read/write.</returns>
    /// <exception cref="Exception">Thrown if the database name is not found on this connection.</exception>
    public bool DbReadOnly(string databaseName = "main")
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        using var utf8Buffer = new Utf8CStringBuffer(databaseName, stackalloc byte[512]);
        int result;
        fixed (byte* pSchema = utf8Buffer)
        {
            result = NativeMethods.sqlite3_db_readonly(Sqlite3Handle, pSchema);
        }
        return result switch
        {
            1 => true,  // Read-Only
            0 => false, // Read-Write
            _ => throw new ArgumentException(
                $"The database '{databaseName}' is not attached to this connection.")
        };
    }

    public ResultCode ErrorCode()
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        int rc = NativeMethods.sqlite3_errcode(Sqlite3Handle);
        return (ResultCode)rc;
    }

    /// <summary>
    /// Returns the latest extended SQLite error code for this connection.
    /// </summary>
    public ResultCode ExtendedErrorCode()
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        int rc = NativeMethods.sqlite3_extended_errcode(Sqlite3Handle);
        return (ResultCode)rc;
    }

    /// <summary>
    /// Returns English-language text that describes the error
    /// or NULL if no error message is available.
    /// </summary>
    public string ErrorMessage()
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        byte* pByte = NativeMethods.sqlite3_errmsg(Sqlite3Handle);
        return Marshal.PtrToStringUTF8((nint)pByte) ?? "Unreadable SQLite error";
    }

    /// <summary>
    /// Returns the English-language text
    /// that describes the [result code] E or NULL if E is not a
    /// result code for which a text error message is available.
    /// </summary>
    public static string ErrorString(ResultCode resultCode)
    {
        byte* pByte = NativeMethods.sqlite3_errstr((int)resultCode);
        return Marshal.PtrToStringUTF8((nint)pByte) ?? "Unknown error code";
    }

    /// <summary>
    /// Returns the byte offset in SQL text where the latest parse error was detected.
    /// </summary>
    /// <returns>The zero-based offset, or -1 if unavailable.</returns>
    public int GetLastErrorOffset()
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        int rtn = NativeMethods.sqlite3_error_offset(Sqlite3Handle);
        return rtn;
    }

    /// <summary>
    /// Sets a busy timeout on this connection.
    /// </summary>
    /// <param name="milliseconds">The timeout in milliseconds.</param>
    public void BusyTimeout(int milliseconds)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        int result = NativeMethods.sqlite3_busy_timeout(Sqlite3Handle, milliseconds);
        if ((ResultCode)result != ResultCode.OK)
            ThrowException((ResultCode)result, ErrorMessage());
    }

    /// <summary>
    /// Interrupts any pending operation running on this connection.
    /// </summary>
    public void Interrupt()
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        NativeMethods.sqlite3_interrupt(Sqlite3Handle);
    }

    /// <summary>
    /// Returns the SQLite library version string used by the native runtime.
    /// </summary>
    /// <returns>
    /// A version string in the form <c>major.minor.patch</c> (for example, <c>3.46.0</c>).
    /// </returns>
    public static string? LibVersion()
    {
        byte* pLibVersion = NativeMethods.sqlite3_libversion();
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
        return NativeMethods.sqlite3_libversion_number();
    }

    public Backup BackupInit(Connection destination,
                             string destinationDatabaseName = "main",
                             string sourceDatabaseName = "main")
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentNullException.ThrowIfNull(destination);
        if (destination.IsClosed || destination.IsInvalid)
            throw new ObjectDisposedException(nameof(Connection));

        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDatabaseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDatabaseName);

        using var destinationNameBuffer = new Utf8CStringBuffer(destinationDatabaseName, stackalloc byte[512]);
        using var sourceNameBuffer = new Utf8CStringBuffer(sourceDatabaseName, stackalloc byte[512]);

        fixed (byte* pDest = destinationNameBuffer, pSource = sourceNameBuffer)
        {

            sqlite3_backup* pBackup = NativeMethods.sqlite3_backup_init(
                destination.Sqlite3Handle,
                pDest,
                Sqlite3Handle,
                pSource);

            if ((nint)pBackup == nint.Zero)
            {
                var result = destination.ErrorCode();
                var errormessage = destination.ErrorMessage();
                ThrowException(result, errormessage);
            }

            return new Backup(pBackup);
        }
    }

    /// <summary>
    /// Opens a BLOB for incremental I/O, identified by database, table, column and rowid.
    /// </summary>
    /// <param name="tableName">The name of the table containing the BLOB.</param>
    /// <param name="columnName">The name of the column containing the BLOB.</param>
    /// <param name="rowId">The rowid of the row containing the BLOB.</param>
    /// <param name="readWrite">If <c>true</c>, opens for read/write; otherwise read-only.</param>
    /// <param name="databaseName">The attached database name (default "main").</param>
    /// <returns>A new <see cref="Blob"/> instance wrapping the open handle.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the connection is no longer valid.</exception>
    /// <exception cref="Exception">Thrown if the BLOB cannot be opened (e.g. row/column not found).</exception>
    public Blob BlobOpen(string tableName,
                         string columnName,
                         long rowId,
                         bool readWrite = false,
                         string databaseName = "main")
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(columnName);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        using var dbBuffer = new Utf8CStringBuffer(databaseName, stackalloc byte[256]);
        using var tableBuffer = new Utf8CStringBuffer(tableName, stackalloc byte[256]);
        using var columnBuffer = new Utf8CStringBuffer(columnName, stackalloc byte[256]);

        fixed (byte* pDb = dbBuffer, pTable = tableBuffer, pColumn = columnBuffer)
        {
            sqlite3_blob* pBlob = default;
            ResultCode result = (ResultCode)NativeMethods.sqlite3_blob_open(
                Sqlite3Handle,
                pDb,
                pTable,
                pColumn,
                rowId,
                readWrite ? 1 : 0,
                &pBlob);

            var blob = new Blob(pBlob, this);

            if (result != ResultCode.OK)
            {
                blob.Dispose();
                var errormessage = ErrorMessage();
                Exception.ThrowException(result, errormessage, $"{nameof(Blob)}.Open on {tableName}.{columnName} (rowid {rowId})");
            }

            return blob;
        }
    }

    #endregion


    #region Other public method

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
    /// <exception cref="Exception">Thrown if the metadata cannot be retrieved.</exception>
    public void GetTableColumnMetadata(string tableName,
                                       string columnName,
                                       out string? dataType,
                                       out string? collSeq,
                                       out bool isNotNull,
                                       out bool isPrimaryKey,
                                       out bool isAutoIncrement)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
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

            fixed (byte* pTableName = tableNameBuffer)
            fixed (byte* pColumnName = columnNameBuffer)
            {
                ResultCode rc = (ResultCode)NativeMethods.sqlite3_table_column_metadata(
                    Sqlite3Handle,
                    null,
                    pTableName,
                    pColumnName,
                    &pDataType,
                    &pCollSeq,
                    &notNull,
                    &primaryKey,
                    &autoInc);

                if (rc != ResultCode.OK)
                {
                    string operation = $"Connection.GetTableColumnMetadata metadata lookup for column '{columnName}' in table '{tableName}'";
                    ThrowException(rc, operation);
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

    #endregion


    #region Private Methods

    [DoesNotReturn]
    private static void ThrowException(ResultCode result, string errorMessage, [CallerMemberName] string caller = "")
    {
        Exception.ThrowException(result, errorMessage, $"{nameof(Connection)}.{caller}");
    }

    #endregion
}
