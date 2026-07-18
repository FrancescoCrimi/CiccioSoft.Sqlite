// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Runtime.InteropServices;
using CiccioSoft.Sqlite.Interop.Native;

namespace CiccioSoft.Sqlite.Interop;

/// <summary>
/// Represents an error returned by the native SQLite interop layer.
/// </summary>
public sealed unsafe class SqliteInteropException : Exception
{
    string? _operation;

    public SqliteInteropException(string message) : base(message) { }

    public SqliteInteropException(SqliteExtendedResult result, string nativeMessage, string operation)
    {
        ExtendedErrorCode = result;
        BaseErrorCode = (SqliteResult)(((int)result) & 0xFF);
        NativeMessage = nativeMessage;
        _operation = operation;
    }

    public SqliteInteropException(SqliteExtendedResult result, Sqlite3SafeHandle sqlite3SafeHandle, string operation)
    {
        ExtendedErrorCode = result;
        BaseErrorCode = (SqliteResult)(((int)result) & 0xFF);
        _operation = operation;

        if (!sqlite3SafeHandle.IsInvalid)
        {
            // Read extended code exactly once from the native connection.
            // ExtendedErrorCode = (SqliteExtendedErrorCode)Sqlite3Native.sqlite3_extended_errcode(sqlite3SafeHandle.AsStructPointer());
            byte* pErr = Sqlite3Native.sqlite3_errmsg(sqlite3SafeHandle.AsStructPointer());
            NativeMessage = Marshal.PtrToStringUTF8((nint)pErr) ?? "Unreadable SQLite error";
        }
        else
        {
            // ExtendedErrorCode = (SqliteExtendedErrorCode)result;
            NativeMessage = "Unknown SQLite error";
        }
    }

    public override string Message =>
        $"{_operation} failed. Base code: {BaseErrorCode}, " +
        $"Extended code: {ExtendedErrorCode} ({(int)ExtendedErrorCode}). " +
        $"Native message: {NativeMessage}";

    /// <summary>
    /// Gets the base SQLite error code (lowest 8 bits).
    /// </summary>
    public SqliteResult BaseErrorCode { get; }

    /// <summary>
    /// Gets the extended SQLite error code.
    /// </summary>
    public SqliteExtendedResult ExtendedErrorCode { get; }

    /// <summary>
    /// Gets the native message returned by SQLite.
    /// </summary>
    public string? NativeMessage { get; }
}
