// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CiccioSoft.Sqlite.Native.Interop;

namespace CiccioSoft.Sqlite.Native;

public sealed unsafe class Statement : SafeHandle
{
    private readonly Connection _connection;
    private readonly bool _isReadOnly;


    #region Ctor and safehandle

    internal Statement(sqlite3_stmt* pStmt, Connection connection)
        : base((nint)pStmt, true)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _isReadOnly = NativeMethods.sqlite3_stmt_readonly(pStmt) != 0;
    }

    public override bool IsInvalid => handle == nint.Zero;

    protected override bool ReleaseHandle()
    {
        _ = NativeMethods.sqlite3_finalize((sqlite3_stmt*)handle);
        return true;
    }

    private sqlite3_stmt* _sqlite3_stmt => (sqlite3_stmt*)DangerousGetHandle();

    #endregion


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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Step()
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        ResultCode result = (ResultCode)NativeMethods.sqlite3_step(_sqlite3_stmt);

        if (result == ResultCode.Row) return true;
        if (result == ResultCode.Done) return false;

        ThrowException(result, _connection.ErrorMessage());
        return false;
    }

    #endregion


    #region Reset A Prepared Statement Object

    /// <summary>
    /// Resets the prepared statement back to its initial state, ready to be re-executed.
    /// </summary>
    /// <exception cref="Exception">Thrown if the reset operation fails.</exception>
    public void Reset()
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        ResultCode result = (ResultCode)NativeMethods.sqlite3_reset(_sqlite3_stmt);

        if (result != ResultCode.OK)
            ThrowException(result, _connection.ErrorMessage());
    }

    #endregion


    #region Reset All Bindings On A Prepared Statement

    /// <summary>
    /// Resets all bound parameters in the prepared statement back to a NULL state.
    /// </summary>
    /// <exception cref="System.Exception">Thrown if the native clearing of bindings fails.</exception>
    public void ClearBindings()
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        ResultCode result = (ResultCode)NativeMethods.sqlite3_clear_bindings(_sqlite3_stmt);

        if (result != ResultCode.OK)
            ThrowException(result, _connection.ErrorMessage());
    }

    #endregion


    #region Number Of Columns In A Result Set

    /// <summary>
    /// Returns the number of columns in the result set returned by the prepared statement.
    /// </summary>
    /// <returns>The total count of result columns.</returns>
    /// <remarks>
    /// <b>Usage Scenario:</b>
    /// This method is typically used in a loop combined with <see cref="ColumnName"/> 
    /// or <see cref="ColumnType"/> to dynamically process query results without 
    /// knowing the table schema in advance.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the statement handle is invalid.</exception>
    public int ColumnCount()
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        int rtn = NativeMethods.sqlite3_column_count(_sqlite3_stmt);
        return rtn;
    }

    /// <summary>
    /// Returns the number of SQL parameters in this prepared statement.
    /// </summary>
    public int BindParameterCount()
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        int rtn = NativeMethods.sqlite3_bind_parameter_count(_sqlite3_stmt);
        return rtn;
    }

    // TODO : fix span
    public ReadOnlySpan<byte> BindParameterNameUtf8(int index)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(index);

        byte* pointer = NativeMethods.sqlite3_bind_parameter_name(_sqlite3_stmt, index);
        ReadOnlySpan<byte> span = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(pointer);
        return span;
    }

    /// <summary>
    /// Returns the name of the N-th SQL parameter in the prepared statement.
    /// Parameters of the form ":AAA" or "@AAA" include the prefix. Anonymous parameters ("?") return null.
    /// </summary>
    /// <param name="index">The one-based index of the SQL parameter (first parameter is 1).</param>
    /// <returns>The name of the parameter, or null if the parameter is nameless or out of range.</returns>
    public string? BindParameterName(int index)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(index);

        byte* pName = NativeMethods.sqlite3_bind_parameter_name(_sqlite3_stmt, index);
        return pName is null ? null : Marshal.PtrToStringUTF8((nint)pName);
    }

    /// <summary>
    /// Returns the one-based index of an SQL parameter given its name.
    /// </summary>
    /// <param name="name">The name of the parameter including its prefix (e.g., ":userName", "@id").</param>
    /// <returns>The one-based index of the parameter, or 0 if no matching parameter is found.</returns>
    public int BindParameterIndex(string parameterName)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        if (string.IsNullOrEmpty(parameterName))
            throw new ArgumentException("Parameter name cannot be null or empty.", nameof(parameterName));

        using var utf8Buffer = new Utf8CStringBuffer(parameterName, stackalloc byte[512]);

        fixed (byte* pBuf = utf8Buffer)
        {
            int index = NativeMethods.sqlite3_bind_parameter_index(_sqlite3_stmt, pBuf);
            return index;
        }
    }

    #endregion


    #region Column Names In A Result Set

    /// <summary>
    /// Retrieves the name of the result column at the specified index.
    /// </summary>
    /// <param name="index">The 0-based index of the column.</param>
    /// <returns>The column name; <c>null</c> if the index is out of range or the name is unavailable.</returns>
    public string? ColumnName(int index)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        // sqlite3_column_name restituisce un byte* UTF-8 (null-terminated)
        byte* pName = NativeMethods.sqlite3_column_name(_sqlite3_stmt, index);

        // Se l'indice è fuori intervallo o il nome non è disponibile, SQLite restituisce NULL
        // Converte il puntatore UTF-8 null-terminated in stringa gestita
        return pName is null ? null : Marshal.PtrToStringUTF8((nint)pName);
    }

    /// <summary>
    /// Returns the declared type for the specified result column, if available.
    /// </summary>
    public string? ColumnDeclType(int index)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        byte* pText = NativeMethods.sqlite3_column_decltype(_sqlite3_stmt, index);
        return pText is null ? null : Marshal.PtrToStringUTF8((nint)pText);
    }

    /// <summary>
    /// Returns the source database name for the specified result column, if available.
    /// </summary>
    public string? ColumnDatabaseName(int index)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        byte* pText = NativeMethods.sqlite3_column_database_name(_sqlite3_stmt, index);
        return pText is null ? null : Marshal.PtrToStringUTF8((nint)pText);
    }

    /// <summary>
    /// Returns the source table name for the specified result column, if available.
    /// </summary>
    public string? ColumnTableName(int index)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        byte* pText = NativeMethods.sqlite3_column_table_name(_sqlite3_stmt, index);
        return pText is null ? null : Marshal.PtrToStringUTF8((nint)pText);
    }

    /// <summary>
    /// Returns the source column name for the specified result column, if available.
    /// </summary>
    public string? ColumnOriginName(int index)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        byte* pText = NativeMethods.sqlite3_column_origin_name(_sqlite3_stmt, index);
        return pText is null ? null : Marshal.PtrToStringUTF8((nint)pText);
    }

    #endregion


    #region Result Values From A Query

    /// <summary>
    /// Retrieves a 32-bit signed integer value from the specified result column.
    /// </summary>
    /// <param name="index">The 0-based index of the column to retrieve.</param>
    /// <returns>The 32-bit integer value of the column.</returns>
    public int ColumnInt(int index)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        int rtn = NativeMethods.sqlite3_column_int(_sqlite3_stmt, index);
        return rtn;
    }

    /// <summary>
    /// Retrieves a 64-bit signed integer value from the specified result column.
    /// </summary>
    /// <param name="index">The 0-based index of the column to retrieve.</param>
    /// <returns>The 64-bit long value of the column.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ColumnInt64(int index)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        long rtn = NativeMethods.sqlite3_column_int64(_sqlite3_stmt, index);
        return rtn;
    }

    /// <summary>
    /// Retrieves a 64-bit floating point value from the specified result column.
    /// </summary>
    /// <param name="index">The 0-based index of the column to retrieve.</param>
    /// <returns>The double-precision value of the column.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ColumnDouble(int index)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        double rtn = NativeMethods.sqlite3_column_double(_sqlite3_stmt, index);
        return rtn;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf8String ColumnTextUtf8(int index)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        // Otteniamo il puntatore alla memoria nativa gestita da SQLite
        byte* ptr = NativeMethods.sqlite3_column_text(_sqlite3_stmt, index);

        if (ptr == null)
            return Utf8String.Utf8StringNull;
        else
        {
            // Chiediamo a SQLite la lunghezza esatta in byte
            int length = NativeMethods.sqlite3_column_bytes(_sqlite3_stmt, index);
            if (length == 0)
                return Utf8String.Utf8StringEmpty;
            else
                return new Utf8String(new ReadOnlySpan<byte>(ptr, length));
        }
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
    /// <exception cref="System.Exception">Thrown if the column cannot be read or the statement is in an invalid state.</exception>
    public string? ColumnText(int index)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        // Otteniamo il puntatore alla memoria nativa gestita da SQLite
        byte* ptr = NativeMethods.sqlite3_column_text(_sqlite3_stmt, index);

        if (ptr == null)
            return null;
        else
        {
            // Chiediamo a SQLite la lunghezza esatta in byte
            int bytelen = NativeMethods.sqlite3_column_bytes(_sqlite3_stmt, index);
            string text = Marshal.PtrToStringUTF8((nint)ptr, bytelen);
            return text;
        }
    }

    /// <summary>
    /// Retrieves a direct view of a result column as a binary large object (BLOB) without copying memory.
    /// </summary>
    /// <param name="index">The 0-based index of the column to retrieve.</param>
    /// <returns>A <see cref="ReadOnlySpan{Byte}"/> pointing directly to the native SQLite memory; <see cref="ReadOnlySpan{Byte}.Empty"/> if NULL.</returns>
    /// <exception cref="System.Exception">Thrown if the column cannot be read or the statement is in an invalid state.</exception>
    public ReadOnlySpan<byte> ColumnBlob(int index)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        // Otteniamo il puntatore alla memoria del BLOB gestita da SQLite
        void* pBlob = NativeMethods.sqlite3_column_blob(_sqlite3_stmt, index);
        // Otteniamo la dimensione in byte
        int length = NativeMethods.sqlite3_column_bytes(_sqlite3_stmt, index);

        // Controllo prima della creazione dello Span per pulizia,
        // anche se in C# passare null con length 0 a ReadOnlySpan è valido.
        if (pBlob == null || length <= 0) return ReadOnlySpan<byte>.Empty;

        // Restituiamo uno Span che punta direttamente alla memoria interna di SQLite.
        // NOTA: Questo Span è valido solo finché non chiami Step() o Reset() sullo statement.
        return new ReadOnlySpan<byte>(pBlob, length);
    }

    /// <summary>
    /// Returns the data type of the value in the specified column for the current row.
    /// Call this only after a successful step that returned a row.
    /// </summary>
    /// <param name="index">The zero-based index of the column.</param>
    /// <returns>The <see cref="SqliteType"/> representing the type of the value.</returns>  
    public SqliteType ColumnType(int index)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        int typeCode = NativeMethods.sqlite3_column_type(_sqlite3_stmt, index);
        return (SqliteType)typeCode;
    }

    /// <summary>
    /// Returns <c>true</c> if this prepared statement is read-only.
    /// </summary>
    public bool IsReadOnly()
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        return _isReadOnly;
    }

    /// <summary>
    /// Returns <c>true</c> if this prepared statement has been stepped but not yet reset/finalized.
    /// </summary>
    public bool IsBusy()
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        int rtn = NativeMethods.sqlite3_stmt_busy(_sqlite3_stmt);
        return rtn != 0;
    }

    /// <summary>
    /// Returns the SQL text of this prepared statement with all bound parameters expanded to their actual values.
    /// </summary>
    /// <returns>The fully expanded SQL string, or null if out of memory or trace is omitted.</returns>
    public string? ExpandedSql()
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        byte* pExpanded = NativeMethods.sqlite3_expanded_sql(_sqlite3_stmt);

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
            NativeMethods.sqlite3_free(pExpanded);
        }
    }

    /// <summary>
    /// Returns the original SQL text used to prepare this statement.
    /// </summary>
    public string? Sql()
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        byte* pSql = NativeMethods.sqlite3_sql(_sqlite3_stmt);
        return pSql is null ? null : Marshal.PtrToStringUTF8((nint)pSql);
    }

    #endregion


    #region Binding Values To Prepared Statements

    /// <summary>
    /// Binds a NULL value to a prepared statement parameter at the specified index.
    /// </summary>
    /// <param name="index">The 1-based index of the parameter to bind.</param>
    public void BindNull(int index)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(index);

        ResultCode result = (ResultCode)NativeMethods.sqlite3_bind_null(_sqlite3_stmt, index);
        if (result != ResultCode.OK)
            ThrowBindException(result, index, _connection.ErrorMessage());
    }

    /// <summary>
    /// Binds a 32-bit signed integer to a prepared statement parameter at the specified index.
    /// </summary>
    /// <param name="index">The 1-based index of the parameter to bind.</param>
    /// <param name="value">The integer value to bind.</param>
    /// <exception cref="Exception">Thrown if the binding operation fails.</exception>
    public void BindInt(int index, int value)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(index);

        ResultCode result = (ResultCode)NativeMethods.sqlite3_bind_int(_sqlite3_stmt, index, value);
        if (result != ResultCode.OK)
            ThrowBindException(result, index, _connection.ErrorMessage());
    }

    /// <summary>
    /// Binds a 64-bit signed integer to a prepared statement parameter at the specified index.
    /// </summary>
    /// <param name="index">The 1-based index of the parameter to bind.</param>
    /// <param name="value">The long value to bind.</param>
    public void BindInt64(int index, long value)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(index);

        ResultCode result = (ResultCode)NativeMethods.sqlite3_bind_int64(_sqlite3_stmt, index, value);
        if (result != ResultCode.OK)
            ThrowBindException(result, index, _connection.ErrorMessage());
    }

    /// <summary>
    /// Binds a 64-bit floating point value to a prepared statement parameter at the specified index.
    /// </summary>
    /// <param name="index">The 1-based index of the parameter to bind.</param>
    /// <param name="value">The double value to bind.</param>
    public void BindDouble(int index, double value)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(index);

        ResultCode result = (ResultCode)NativeMethods.sqlite3_bind_double(_sqlite3_stmt, index, value);
        if (result != ResultCode.OK)
            ThrowBindException(result, index, _connection.ErrorMessage());
    }

    /// <summary>
    /// Binds a string value to a prepared statement parameter at the specified index.
    /// </summary>
    /// <param name="index">The 1-based index of the parameter to bind.</param>
    /// <param name="text">The string value to bind. If null, a SQL NULL is bound instead.</param>
    /// <exception cref="System.Exception">Thrown if the binding fails or the statement is invalid.</exception>
    public void BindText(int index, string? text)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(index);

        ResultCode result;
        using var utf8Buffer = new Utf8CStringBuffer(text, stackalloc byte[1024]);
        fixed (byte* pBuf = utf8Buffer)
        {
            result = (ResultCode)NativeMethods.sqlite3_bind_text(
              _sqlite3_stmt,
              index,
              pBuf,
              utf8Buffer.Length,
              NativeMethods.SQLITE_TRANSIENT); // -1 = SQLITE_TRANSIENT
        }

        if (result != ResultCode.OK)
            ThrowBindException(result, index, _connection.ErrorMessage());
    }

    public void BindText(int index, ReadOnlySpan<byte> text)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(index);

        ResultCode result;
        if (text.IsEmpty)
        {
            byte dummy = 0;
            result = (ResultCode)NativeMethods.sqlite3_bind_text(
              _sqlite3_stmt,
              index,
              &dummy,
              0,
              NativeMethods.SQLITE_TRANSIENT);
        }
        else
        {
            fixed (byte* pBuf = text)
            {
                result = (ResultCode)NativeMethods.sqlite3_bind_text(
                  _sqlite3_stmt,
                  index,
                  pBuf,
                  text.Length,
                  NativeMethods.SQLITE_TRANSIENT); // -1 = SQLITE_TRANSIENT
            }
        }

        if (result != ResultCode.OK)
            ThrowBindException(result, index, _connection.ErrorMessage());
    }

    /// <summary>
    /// Binds a binary large object (BLOB) to a prepared statement parameter at the specified index.
    /// </summary>
    /// <param name="index">The 1-based index of the parameter to bind.</param>
    /// <param name="data">
    /// The binary data to bind as a <see cref="ReadOnlySpan{Byte}"/>.
    /// </param>
    /// <exception cref="System.Exception">Thrown if the binding fails or the statement is in an invalid state.</exception>
    public void BindBlob(int index, ReadOnlySpan<byte> data)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(index);

        fixed (byte* pData = data)
        {
            ResultCode result = (ResultCode)NativeMethods.sqlite3_bind_blob(
               _sqlite3_stmt,
               index,
               pData,
               data.Length,
               NativeMethods.SQLITE_TRANSIENT);
            if (result != ResultCode.OK)
                ThrowBindException(result, index, _connection.ErrorMessage());
        }
    }

    #endregion


    #region Private Methods

    [DoesNotReturn]
    private void ThrowBindException(ResultCode result, int index, string errorMessage, [CallerMemberName] string caller = "")
    {
        Exception.ThrowException(result, errorMessage, $"{nameof(Statement)}.{caller} to parameter index {index}");
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowException(ResultCode result, string errorMessage, [CallerMemberName] string caller = "")
    {
        Exception.ThrowException(result, errorMessage, $"{nameof(Statement)}.{caller}");
    }

    #endregion
}
