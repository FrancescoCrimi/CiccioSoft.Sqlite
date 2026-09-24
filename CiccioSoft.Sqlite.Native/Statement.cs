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

    private sqlite3_stmt* Sqlite3StatementHandle => (sqlite3_stmt*)DangerousGetHandle();

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

        ResultCode result = (ResultCode)NativeMethods.sqlite3_step(Sqlite3StatementHandle);

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

        ResultCode result = (ResultCode)NativeMethods.sqlite3_reset(Sqlite3StatementHandle);

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

        ResultCode result = (ResultCode)NativeMethods.sqlite3_clear_bindings(Sqlite3StatementHandle);

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
    /// This method is typically used in a loop combined with <see cref="GetColumnName"/> 
    /// or <see cref="GetColumnType"/> to dynamically process query results without 
    /// knowing the table schema in advance.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the statement handle is invalid.</exception>
    public int ColumnCount()
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        int rtn = NativeMethods.sqlite3_column_count(Sqlite3StatementHandle);
        return rtn;
    }

    /// <summary>
    /// Returns the number of SQL parameters in this prepared statement.
    /// </summary>
    public int ParameterCount()
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        int rtn = NativeMethods.sqlite3_bind_parameter_count(Sqlite3StatementHandle);
        return rtn;
    }

    // TODO : fix span
    public ReadOnlySpan<byte> GetParameterName(int index)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(index);

        byte* pByte = NativeMethods.sqlite3_bind_parameter_name(Sqlite3StatementHandle, index);

        if (pByte == null)
            return ReadOnlySpan<byte>.Empty;

        int length = 0;
        while (pByte[length] != 0) length++;
        return new ReadOnlySpan<byte>(pByte, length);
    }

    /// <summary>
    /// Returns the name of the N-th SQL parameter in the prepared statement.
    /// Parameters of the form ":AAA" or "@AAA" include the prefix. Anonymous parameters ("?") return null.
    /// </summary>
    /// <param name="index">The one-based index of the SQL parameter (first parameter is 1).</param>
    /// <returns>The name of the parameter, or null if the parameter is nameless or out of range.</returns>
    public string? GetParameterNameString(int index)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(index);

        byte* pName = NativeMethods.sqlite3_bind_parameter_name(Sqlite3StatementHandle, index);
        return pName is null ? null : Marshal.PtrToStringUTF8((nint)pName);
    }

    /// <summary>
    /// Returns the one-based index of an SQL parameter given its name.
    /// </summary>
    /// <param name="name">The name of the parameter including its prefix (e.g., ":userName", "@id").</param>
    /// <returns>The one-based index of the parameter, or 0 if no matching parameter is found.</returns>
    public int GetParameterIndex(string parameterName)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        if (string.IsNullOrEmpty(parameterName))
            throw new ArgumentException("Parameter name cannot be null or empty.", nameof(parameterName));

        using var utf8Buffer = new Utf8CStringBuffer(parameterName, stackalloc byte[512]);

        fixed (byte* pBuf = utf8Buffer)
        {
            int rtn = NativeMethods.sqlite3_bind_parameter_index(Sqlite3StatementHandle, pBuf);
            return rtn;
        }
    }

    #endregion


    #region Column Names In A Result Set

    /// <summary>
    /// Retrieves the name of the result column at the specified index.
    /// </summary>
    /// <param name="index">The 0-based index of the column.</param>
    /// <returns>The column name; <c>null</c> if the index is out of range or the name is unavailable.</returns>
    public string? GetColumnName(int index)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        // sqlite3_column_name restituisce un byte* UTF-8 (null-terminated)
        byte* pName = NativeMethods.sqlite3_column_name(Sqlite3StatementHandle, index);

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
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        byte* pText = NativeMethods.sqlite3_column_decltype(Sqlite3StatementHandle, index);
        return pText is null ? null : Marshal.PtrToStringUTF8((nint)pText);
    }

    /// <summary>
    /// Returns the source database name for the specified result column, if available.
    /// </summary>
    public string? GetColumnDatabaseName(int index)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        byte* pText = NativeMethods.sqlite3_column_database_name(Sqlite3StatementHandle, index);
        return pText is null ? null : Marshal.PtrToStringUTF8((nint)pText);
    }

    /// <summary>
    /// Returns the source table name for the specified result column, if available.
    /// </summary>
    public string? GetColumnTableName(int index)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        byte* pText = NativeMethods.sqlite3_column_table_name(Sqlite3StatementHandle, index);
        return pText is null ? null : Marshal.PtrToStringUTF8((nint)pText);
    }

    /// <summary>
    /// Returns the source column name for the specified result column, if available.
    /// </summary>
    public string? GetColumnOriginName(int index)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        byte* pText = NativeMethods.sqlite3_column_origin_name(Sqlite3StatementHandle, index);
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
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        int rtn = NativeMethods.sqlite3_column_int(Sqlite3StatementHandle, index);
        return rtn;
    }

    /// <summary>
    /// Retrieves a 64-bit signed integer value from the specified result column.
    /// </summary>
    /// <param name="index">The 0-based index of the column to retrieve.</param>
    /// <returns>The 64-bit long value of the column.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long GetLong(int index)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        long rtn = NativeMethods.sqlite3_column_int64(Sqlite3StatementHandle, index);
        return rtn;
    }

    /// <summary>
    /// Retrieves a 64-bit floating point value from the specified result column.
    /// </summary>
    /// <param name="index">The 0-based index of the column to retrieve.</param>
    /// <returns>The double-precision value of the column.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetDouble(int index)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        double rtn = NativeMethods.sqlite3_column_double(Sqlite3StatementHandle, index);
        return rtn;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> GetTextAsSpan(int index)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        // Otteniamo il puntatore alla memoria nativa gestita da SQLite
        byte* pText = NativeMethods.sqlite3_column_text(Sqlite3StatementHandle, index);
        // Chiediamo a SQLite la lunghezza esatta in byte
        int length = NativeMethods.sqlite3_column_bytes(Sqlite3StatementHandle, index);

        if (pText == null) return ReadOnlySpan<byte>.Empty;
        if (length == 0) return ReadOnlySpan<byte>.Empty;
        return new ReadOnlySpan<byte>(pText, length);
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
    public string? GetText(int index)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        // Otteniamo il puntatore alla memoria nativa gestita da SQLite
        byte* pText = NativeMethods.sqlite3_column_text(Sqlite3StatementHandle, index);

        // Marshal.PtrToStringUTF8 gestisce internamente il controllo null e la terminazione \0
        return pText == null ? null : Marshal.PtrToStringUTF8((nint)pText);
    }

    /// <summary>
    /// Retrieves a direct view of a result column as a binary large object (BLOB) without copying memory.
    /// </summary>
    /// <param name="index">The 0-based index of the column to retrieve.</param>
    /// <returns>A <see cref="ReadOnlySpan{Byte}"/> pointing directly to the native SQLite memory; <see cref="ReadOnlySpan{Byte}.Empty"/> if NULL.</returns>
    /// <exception cref="System.Exception">Thrown if the column cannot be read or the statement is in an invalid state.</exception>
    public ReadOnlySpan<byte> GetBlob(int index)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        // Otteniamo il puntatore alla memoria del BLOB gestita da SQLite
        void* pBlob = NativeMethods.sqlite3_column_blob(Sqlite3StatementHandle, index);
        // Otteniamo la dimensione in byte
        int length = NativeMethods.sqlite3_column_bytes(Sqlite3StatementHandle, index);

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
    public SqliteType GetColumnType(int index)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        int typeCode = NativeMethods.sqlite3_column_type(Sqlite3StatementHandle, index);
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

        int rtn = NativeMethods.sqlite3_stmt_busy(Sqlite3StatementHandle);
        return rtn != 0;
    }

    /// <summary>
    /// Returns the SQL text of this prepared statement with all bound parameters expanded to their actual values.
    /// </summary>
    /// <returns>The fully expanded SQL string, or null if out of memory or trace is omitted.</returns>
    public string? GetExpandedSql()
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        byte* pExpanded = NativeMethods.sqlite3_expanded_sql(Sqlite3StatementHandle);

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
    public string? GetSql()
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        byte* pSql = NativeMethods.sqlite3_sql(Sqlite3StatementHandle);
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

        int result = NativeMethods.sqlite3_bind_null(Sqlite3StatementHandle, index);
        if ((ResultCode)result != ResultCode.OK)
            ThrowBindException((ResultCode)result, index, _connection.ErrorMessage());
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

        int result = NativeMethods.sqlite3_bind_int(Sqlite3StatementHandle, index, value);
        if ((ResultCode)result != ResultCode.OK)
            ThrowBindException((ResultCode)result, index, _connection.ErrorMessage());
    }

    /// <summary>
    /// Binds a 64-bit signed integer to a prepared statement parameter at the specified index.
    /// </summary>
    /// <param name="index">The 1-based index of the parameter to bind.</param>
    /// <param name="value">The long value to bind.</param>
    public void BindLong(int index, long value)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(index);

        int result = NativeMethods.sqlite3_bind_int64(Sqlite3StatementHandle, index, value);
        if ((ResultCode)result != ResultCode.OK)
            ThrowBindException((ResultCode)result, index, _connection.ErrorMessage());
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

        int result = NativeMethods.sqlite3_bind_double(Sqlite3StatementHandle, index, value);
        if ((ResultCode)result != ResultCode.OK)
            ThrowBindException((ResultCode)result, index, _connection.ErrorMessage());
    }

    /// <summary>
    /// Binds a string value to a prepared statement parameter at the specified index.
    /// </summary>
    /// <param name="index">The 1-based index of the parameter to bind.</param>
    /// <param name="text">The string value to bind. If null, a SQL NULL is bound instead.</param>
    /// <exception cref="System.Exception">Thrown if the binding fails or the statement is invalid.</exception>
    public void BindText(int index, string text)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(index);

        using var utf8Buffer = new Utf8CStringBuffer(text, stackalloc byte[1024]);

        var resultCode = BindTextCore(index, utf8Buffer.AsSpan());
        if (resultCode != ResultCode.OK)
            ThrowBindException(resultCode, index, _connection.ErrorMessage());
    }

    public void BindText(int index, ReadOnlySpan<byte> text)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(index);

        var resultCode = BindTextCore(index, text);
        if (resultCode != ResultCode.OK)
            ThrowBindException(resultCode, index, _connection.ErrorMessage());
    }

    /// <summary>
    /// Esegue il pinning di <paramref name="text"/> e invoca <c>sqlite3_bind_text</c>.
    /// </summary>
    private ResultCode BindTextCore(int index, ReadOnlySpan<byte> text)
    {
        fixed (byte* pBuf = text)
        {
            int result = NativeMethods.sqlite3_bind_text(
               Sqlite3StatementHandle,
               index,
               pBuf,
               text.Length,
               NativeMethods.SQLITE_TRANSIENT); // -1 = SQLITE_TRANSIENT
            return (ResultCode)result;
        }
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
               Sqlite3StatementHandle,
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
