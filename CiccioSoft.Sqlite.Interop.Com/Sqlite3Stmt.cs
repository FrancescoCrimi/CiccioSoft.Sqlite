// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace CiccioSoft.Sqlite.Interop.Com;

public sealed unsafe class Sqlite3StmtHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    internal Sqlite3StmtHandle(nint handle) : base(true)
    {
        SetHandle(handle);
    }
    protected override bool ReleaseHandle()
    {
        return (SqliteResult)VTableCache.Instance.Stmt.finalize(handle) == SqliteResult.OK;
    }
}

public sealed unsafe class Sqlite3Stmt : IDisposable
{
    private readonly Sqlite3StmtHandle _handle;
    private static VTableCache _vtable = VTableCache.Instance;


    internal Sqlite3Stmt(Sqlite3StmtHandle handle)
    {
        _handle = handle;
    }


    #region Evaluate An SQL Statement

    /// <summary>
    /// Advances the prepared statement to the next row of the result set.
    /// </summary>
    /// <returns><c>true</c> if a new row of data is available; <c>false</c> if the execution has completed successfully.</returns>
    /// <remarks>
    /// <b>Control Flow:</b>
    /// - <c>SQLITE_ROW</c>: Data is ready to be read via Column methods.
    /// - <c>SQLITE_DONE</c>: Query finished or an INSERT/UPDATE/DELETE was executed.
    /// </remarks>
    /// <exception cref="Exception">Thrown if an error occurs during execution (e.g., constraint violations).</exception>
    public SqliteResult Step()
    {
        SqliteResult res = (SqliteResult)_vtable.Stmt.step(_handle.DangerousGetHandle());
        return res;
    }

    #endregion


    #region Reset A Prepared Statement Object

    /// <summary>
    /// Resets the prepared statement back to its initial state, ready to be re-executed.
    /// </summary>
    /// <remarks>
    /// <b>Performance Note:</b> 
    /// Resetting a statement is significantly faster than finalizing and re-preparing it. 
    /// It retains bound parameters unless <c>sqlite3_clear_bindings</c> is explicitly called.
    /// </remarks>
    /// <exception cref="Exception">Thrown if the reset operation fails.</exception>
    public SqliteResult Reset()
    {
        SqliteResult res = (SqliteResult)_vtable.Stmt.reset(_handle.DangerousGetHandle());
        return res;
    }

    #endregion


    #region Reset All Bindings On A Prepared Statement

    /// <summary>
    /// Resets all bound parameters in the prepared statement back to a NULL state.
    /// </summary>
    /// <remarks>
    /// <b>Resource Management:</b>
    /// - Zero-Allocation: Resets native parameter slots without allocating new managed objects.
    /// - Optimization: Useful when reusing the same statement across multiple executions with different parameter sets.
    /// </remarks>
    /// <exception cref="Exception">Thrown if the native clearing of bindings fails.</exception>
    public SqliteResult ClearBindings()
    {
        SqliteResult res = (SqliteResult)_vtable.Stmt.clear_bindings(_handle.DangerousGetHandle());
        return res;
    }

    #endregion


    #region Number Of Columns In A Result Set

    /// <summary>
    /// Returns the number of columns in the result set returned by the prepared statement.
    /// </summary>
    /// <returns>The total count of result columns.</returns>
    /// <remarks>
    /// <b>Usage Scenario:</b>
    /// This method is typically used in a loop combined with <see cref="GetColumnName"/> 
    /// or <see cref="GetColumnType"/> to dynamically process query results without 
    /// knowing the table schema in advance.
    /// </remarks>
    public int ColumnCount()
    {
        return _vtable.Stmt.column_count(_handle.DangerousGetHandle());
    }

    /// <summary>
    /// Returns the number of SQL parameters in this prepared statement.
    /// </summary>
    public int ParameterCount()
    {
        // ThrowIfInvalid();
        return _vtable.Stmt.bind_parameter_count(_handle.DangerousGetHandle());
    }

    /// <summary>
    /// Returns the name of the SQL parameter at the specified 1-based index.
    /// </summary>
    public string? GetParameterName(int index)
    {
        // ThrowIfInvalid();
        byte* pName = _vtable.Stmt.bind_parameter_name(_handle.DangerousGetHandle(), index);
        return pName is null ? null : Marshal.PtrToStringUTF8((nint)pName);
    }

    /// <summary>
    /// Returns the 1-based index for a named SQL parameter (for example <c>@name</c> or <c>:name</c>).
    /// </summary>
    public int GetParameterIndex(string parameterName)
    {
        ArgumentNullException.ThrowIfNull(parameterName);

        using var utf8Buffer = new Utf8SafeStackBuffer(parameterName, stackalloc byte[512]);

        fixed (byte* pBuf = utf8Buffer)
        {
            return _vtable.Stmt.bind_parameter_index(_handle.DangerousGetHandle(), pBuf);
        }
    }

    #endregion


    #region Column Names In A Result Set

    /// <summary>
    /// Retrieves the name of the result column at the specified index.
    /// </summary>
    /// <param name="index">The 0-based index of the column.</param>
    /// <returns>The column name; <c>null</c> if the index is out of range or the name is unavailable.</returns>
    /// <remarks>
    /// <b>Implementation Note:</b>
    /// Uses <see cref="System.Runtime.InteropServices.Marshal.PtrToStringUTF8(IntPtr)"/> to efficiently scan 
    /// for the null terminator and decode the native UTF-8 string into a managed UTF-16 string.
    /// </remarks>
    public string? GetColumnName(int index)
    {
        // sqlite3_column_name restituisce un byte* UTF-8 (null-terminated)
        byte* pName = _vtable.Stmt.column_name(_handle.DangerousGetHandle(), index);

        // Se l'indice è fuori intervallo o il nome non è disponibile, SQLite restituisce NULL
        if (pName == null) return null;

        // Converte il puntatore UTF-8 null-terminated in stringa gestita
        return Marshal.PtrToStringUTF8((nint)pName);
    }

    /// <summary>
    /// Returns the declared type for the specified result column, if available.
    /// </summary>
    public string? GetColumnDeclType(int index)
    {
        byte* pText = _vtable.Stmt.column_decltype(_handle.DangerousGetHandle(), index);
        return pText is null ? null : Marshal.PtrToStringUTF8((nint)pText);
    }

    /// <summary>
    /// Returns the source database name for the specified result column, if available.
    /// </summary>
    public string? GetColumnDatabaseName(int index)
    {
        byte* pText = _vtable.Stmt.column_database_name(_handle.DangerousGetHandle(), index);
        return pText is null ? null : Marshal.PtrToStringUTF8((nint)pText);
    }

    /// <summary>
    /// Returns the source table name for the specified result column, if available.
    /// </summary>
    public string? GetColumnTableName(int index)
    {
        byte* pText = _vtable.Stmt.column_table_name(_handle.DangerousGetHandle(), index);
        return pText is null ? null : Marshal.PtrToStringUTF8((nint)pText);
    }

    /// <summary>
    /// Returns the source column name for the specified result column, if available.
    /// </summary>
    public string? GetColumnOriginName(int index)
    {
        byte* pText = _vtable.Stmt.column_origin_name(_handle.DangerousGetHandle(), index);
        return pText is null ? null : Marshal.PtrToStringUTF8((nint)pText);
    }

    #endregion


    #region Result Values From A Query

    /// <summary>
    /// Retrieves a 32-bit signed integer value from the specified result column.
    /// </summary>
    /// <param name="index">The 0-based index of the column to retrieve.</param>
    /// <returns>The 32-bit integer value of the column.</returns>
    public int GetInt(int index)
    {
        return _vtable.Stmt.column_int(_handle.DangerousGetHandle(), index);
    }

    /// <summary>
    /// Retrieves a 64-bit signed integer value from the specified result column.
    /// </summary>
    /// <param name="index">The 0-based index of the column to retrieve.</param>
    /// <returns>The 64-bit long value of the column.</returns>
    public long GetLong(int index)
    {
        return _vtable.Stmt.column_int64(_handle.DangerousGetHandle(), index);
    }

    /// <summary>
    /// Retrieves a 64-bit floating point value from the specified result column.
    /// </summary>
    /// <param name="index">The 0-based index of the column to retrieve.</param>
    /// <returns>The double-precision value of the column.</returns>
    public double GetDouble(int index)
    {
        return _vtable.Stmt.column_double(_handle.DangerousGetHandle(), index);
    }

    public ReadOnlySpan<byte> GetText(int index)
    {
        // Otteniamo il puntatore alla memoria nativa gestita da SQLite
        byte* pText = _vtable.Stmt.column_text(_handle.DangerousGetHandle(), index);
        if (pText == null) return ReadOnlySpan<byte>.Empty;

        // Chiediamo a SQLite la lunghezza esatta in byte
        int byteCount = _vtable.Stmt.column_bytes(_handle.DangerousGetHandle(), index);
        if (byteCount == 0) return ReadOnlySpan<byte>.Empty;

        return new ReadOnlySpan<byte>(pText, byteCount);
    }

    /// <summary>
    /// Retrieves the value of a result column as a managed string, distinguishing between NULL and empty values.
    /// </summary>
    /// <param name="index">The 0-based index of the column to retrieve.</param>
    /// <returns>
    /// The string value of the column; 
    /// <c>null</c> if the database value is SQL NULL; 
    /// <see cref="string.Empty"/> if the database value is an empty string.
    /// </returns>
    public string? GetTextString(int index)
    {
        // Otteniamo il puntatore alla memoria nativa gestita da SQLite
        byte* pText = _vtable.Stmt.column_text(_handle.DangerousGetHandle(), index);

        // Marshal.PtrToStringUTF8 gestisce internamente il controllo null e la terminazione \0
        return pText == null ? null : Marshal.PtrToStringUTF8((nint)pText);
    }

    /// <summary>
    /// Retrieves a direct view of a result column as a binary large object (BLOB) without copying memory.
    /// </summary>
    /// <param name="index">The 0-based index of the column to retrieve.</param>
    /// <returns>A <see cref="ReadOnlySpan{Byte}"/> pointing directly to the native SQLite memory; <see cref="ReadOnlySpan{Byte}.Empty"/> if NULL.</returns>
    /// <remarks>
    /// <b>Critical Performance Warning:</b>
    /// - Zero-Copy: This method provides direct access to SQLite's internal buffers for maximum speed and zero GC pressure.
    /// - Lifetime: The returned <see cref="ReadOnlySpan{Byte}"/> is <b>only valid</b> until the next call to <c>Step()</c>, <c>Reset()</c>, or <c>Dispose()</c> on this statement.
    /// - Persistence: If you need to keep the data beyond the current row, you must call <c>.ToArray()</c> or copy it to another buffer.
    /// </remarks>
    public ReadOnlySpan<byte> GetBlob(int index)
    {
        // Otteniamo il puntatore alla memoria del BLOB gestita da SQLite
        void* pBlob = _vtable.Stmt.column_blob(_handle.DangerousGetHandle(), index);
        if (pBlob == null) return ReadOnlySpan<byte>.Empty;

        // Otteniamo la dimensione in byte
        int length = _vtable.Stmt.column_bytes(_handle.DangerousGetHandle(), index);

        // Restituiamo uno Span che punta direttamente alla memoria interna di SQLite.
        // NOTA: Questo Span è valido solo finché non chiami Step() o Reset() sullo statement.
        return new ReadOnlySpan<byte>(pBlob, length);
    }

    /// <summary>
    /// Retrieves the fundamental data type of the result column at the specified index.
    /// </summary>
    /// <param name="index">The 0-based index of the column to query.</param>
    /// <returns>
    /// An integer representing the SQLite data type:
    /// <list type="bullet">
    /// <item><description>1: SQLITE_INTEGER</description></item>
    /// <item><description>2: SQLITE_FLOAT</description></item>
    /// <item><description>3: SQLITE_TEXT</description></item>
    /// <item><description>4: SQLITE_BLOB</description></item>
    /// <item><description>5: SQLITE_NULL</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// SQLite uses dynamic typing; the type of a column may change from row to row. 
    /// Call this after <see cref="Step"/> to determine which <c>Get</c> method to use.
    /// </remarks>
    public SqliteType GetColumnType(int index)
    {
        int rc = _vtable.Stmt.column_type(_handle.DangerousGetHandle(), index);
        return (SqliteType)rc;
    }

    /// <summary>
    /// Returns <c>true</c> if this prepared statement is read-only.
    /// </summary>
    public bool IsReadOnly()
    {
        return _vtable.Stmt.stmt_readonly(_handle.DangerousGetHandle()) != 0;
    }

    /// <summary>
    /// Returns <c>true</c> if this prepared statement has been stepped but not yet reset/finalized.
    /// </summary>
    public bool IsBusy()
    {
        return _vtable.Stmt.stmt_busy(_handle.DangerousGetHandle()) != 0;
    }

    /// <summary>
    /// Returns the expanded SQL text with currently bound parameters.
    /// </summary>
    public string? GetExpandedSql()
    {
        byte* pExpanded = _vtable.Stmt.expanded_sql(_handle.DangerousGetHandle());
        if (pExpanded == null)
        {
            return null;
        }

        try
        {
            return Marshal.PtrToStringUTF8((nint)pExpanded);
        }
        finally
        {
            _vtable.Db.free(pExpanded);
        }
    }

    /// <summary>
    /// Returns the original SQL text used to prepare this statement.
    /// </summary>
    public string? GetSql()
    {
        byte* pSql = _vtable.Stmt.sql(_handle.DangerousGetHandle());
        return pSql is null ? null : Marshal.PtrToStringUTF8((nint)pSql);
    }

    #endregion


    #region Binding Values To Prepared Statements

    /// <summary>
    /// Binds a NULL value to a prepared statement parameter at the specified index.
    /// </summary>
    /// <param name="index">The 1-based index of the parameter to bind.</param>
    public SqliteResult BindNull(int index)
    {
        return (SqliteResult)_vtable.Stmt.bind_null(_handle.DangerousGetHandle(), index);
    }

    /// <summary>
    /// Binds a 32-bit signed integer to a prepared statement parameter at the specified index.
    /// </summary>
    /// <param name="index">The 1-based index of the parameter to bind.</param>
    /// <param name="value">The integer value to bind.</param>
    public SqliteResult BindInt(int index, int value)
    {
        return (SqliteResult)_vtable.Stmt.bind_int(_handle.DangerousGetHandle(), index, value);
    }

    /// <summary>
    /// Binds a 64-bit signed integer to a prepared statement parameter at the specified index.
    /// </summary>
    /// <param name="index">The 1-based index of the parameter to bind.</param>
    /// <param name="value">The long value to bind.</param>
    public SqliteResult BindLong(int index, long value)
    {
        return (SqliteResult)_vtable.Stmt.bind_int64(_handle.DangerousGetHandle(), index, value);
    }

    /// <summary>
    /// Binds a 64-bit floating point value to a prepared statement parameter at the specified index.
    /// </summary>
    /// <param name="index">The 1-based index of the parameter to bind.</param>
    /// <param name="value">The double value to bind.</param>
    public SqliteResult BindDouble(int index, double value)
    {
        return (SqliteResult)_vtable.Stmt.bind_double(_handle.DangerousGetHandle(), index, value);
    }

    /// <summary>
    /// Binds a string value to a prepared statement parameter at the specified index.
    /// </summary>
    /// <param name="index">The 1-based index of the parameter to bind.</param>
    /// <param name="s">The string value to bind. If null, a SQL NULL is bound instead.</param>
    /// <remarks>
    /// <b>High-Performance Implementation:</b>
    /// - Hybrid Allocation: Uses <c>stackalloc</c> for strings up to 1KB to avoid heap allocations.
    /// - Memory Fallback: Uses <see cref="ArrayPool{T}"/> for larger strings to minimize GC pressure.
    /// - Data Lifecycle: Passes <c>SQLITE_TRANSIENT</c> to SQLite, forcing it to make an internal copy of the data, which is necessary since our buffers are reclaimed immediately after the call.
    /// </remarks>
    public SqliteResult BindText(int index, string text)
    {
        // Se la stringa è nulla, bindiamo NULL.
        // Una stringa vuota deve restare una stringa vuota, non SQL NULL.
        if (text is null)
        {
            return (SqliteResult)_vtable.Stmt.bind_null(_handle.DangerousGetHandle(), index);
        }

        // Alloca la memoria base nello stack
        // 512 byte bastano per la maggior parte delle stringhe standard
        using var utf8Buffer = new Utf8SafeStackBuffer(text, stackalloc byte[512]);

        return BindText(index, utf8Buffer.AsSpan());
    }

    public SqliteResult BindText(int index, ReadOnlySpan<byte> text)
    {
        // Se lo span è vuoto, possiamo decidere se bindare NULL o un blob vuoto (zero length)
        if (text.IsEmpty)
            return BindNull(index);

        fixed (byte* pBuf = text)
        {
            SqliteResult res = (SqliteResult)_vtable.Stmt.bind_text(
                _handle.DangerousGetHandle(),
                index,
                pBuf,
                text.Length);
            return res;
        }
    }

    /// <summary>
    /// Binds a binary large object (BLOB) to a prepared statement parameter at the specified index.
    /// </summary>
    /// <param name="index">The 1-based index of the parameter to bind.</param>
    /// <param name="data">The binary data to bind as a <see cref="ReadOnlySpan{Byte}"/>. If empty, a SQL NULL is bound.</param>
    /// <remarks>
    /// <b>Memory Strategy:</b>
    /// - Zero-Copy Entry: Accepts <see cref="ReadOnlySpan{Byte}"/> to allow binding slices of arrays or stack-allocated memory without intermediate allocations.
    /// - Safe P/Invoke: Uses the <c>fixed</c> statement to obtain a stable pointer to the span's underlying memory.
    /// - Persistence: Passes <c>SQLITE_TRANSIENT</c> to ensure SQLite creates an internal copy of the data, safeguarding against the caller reclaiming the memory immediately after the call.
    /// </remarks>
    public SqliteResult BindBlob(int index, ReadOnlySpan<byte> data)
    {
        // Se lo span è vuoto, possiamo decidere se bindare NULL o un blob vuoto (zero length)
        if (data.IsEmpty)
            return BindNull(index);

        fixed (byte* pData = data)
        {
            SqliteResult res = (SqliteResult)_vtable.Stmt.bind_blob(
                _handle.DangerousGetHandle(),
                index,
                pData,
                data.Length);
            return res;
        }
    }

    #endregion


    #region Private Methods

    // private nint GetDbHandle()
    // {
    //     return Sqlite3Native.sqlite3_db_handle(_handle.DangerousGetHandle());
    // }

    #endregion


    public void Dispose() => _handle.Dispose();
}
