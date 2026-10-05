// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CiccioSoft.Sqlite.Native;
using NativeStatement = CiccioSoft.Sqlite.Native.Statement;

namespace CiccioSoft.Sqlite;

public sealed class Statement : IDisposable
{
    private readonly NativeStatement _nativeStatement;
    private readonly Connection _connection;

    public Statement(NativeStatement nativeStatement, Connection connection)
    {
        _nativeStatement = nativeStatement;
        _connection = connection;
    }


    #region Evaluate An SQL Statement

    // /// <summary>
    // /// Advances the prepared statement to the next row of the result set.
    // /// </summary>
    // /// <returns><c>true</c> if a new row of data is available; <c>false</c> if the execution has completed successfully.</returns>
    // /// <remarks>
    // /// <b>Control Flow:</b>
    // /// - <c>SQLITE_ROW</c>: Data is ready to be read via Column methods.
    // /// - <c>SQLITE_DONE</c>: Query finished or an INSERT/UPDATE/DELETE was executed.
    // /// </remarks>
    // /// <exception cref="Exception">Thrown if an error occurs during execution (e.g., constraint violations).</exception>
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public bool Step()
    // {
    //     // ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

    //     ResultCode result = (ResultCode)_nativeStatement.Step();

    //     if (result == ResultCode.Row) return true;
    //     if (result == ResultCode.Done) return false;

    //     ThrowException(result, _connection.ErrorMessage());
    //     return false;
    // }

    #endregion



    #region Result Values From A Query


    public string? GetText(int index)
    {
        return _nativeStatement.ColumnText(index);
    }

    public Utf8String GetTextUtf8(int index)
    {
        return _nativeStatement.ColumnTextUtf8(index);
    }

    #endregion


    #region Binding Values To Prepared Statements

    public void BindText(int index, string? text)
    {
        throw new NotImplementedException();
    }

    public void BindText(int index, ReadOnlySpan<byte> text)
    {

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


    public void Dispose()
    {
        _nativeStatement.Dispose();
    }
}
