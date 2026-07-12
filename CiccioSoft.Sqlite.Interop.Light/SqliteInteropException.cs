// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CiccioSoft.Sqlite.Interop.Native;

namespace CiccioSoft.Sqlite.Interop.Light;

/// <summary>
/// Represents an error returned by the native SQLite interop layer.
/// </summary>
public sealed class SqliteInteropException : Exception
{
    public SqliteInteropException(
        // string message,
        SqliteResult baseErrorCode,
        SqliteExtendedErrorCode extendedErrorCode,
        string nativeMessage,
        string operation)
        : base()
    {
        BaseErrorCode = baseErrorCode;
        ExtendedErrorCode = extendedErrorCode;
        NativeMessage = nativeMessage;
        Operation = operation;
    }

    /// <summary>
    /// Gets the base SQLite error code (lowest 8 bits).
    /// </summary>
    public SqliteResult BaseErrorCode { get; }

    /// <summary>
    /// Gets the extended SQLite error code.
    /// </summary>
    public SqliteExtendedErrorCode ExtendedErrorCode { get; }

    /// <summary>
    /// Gets the native message returned by SQLite.
    /// </summary>
    public string NativeMessage { get; }

    public string Operation { get; }

    public override string Message =>
        $"{Operation} failed. Base code: {BaseErrorCode}, " +
        $"Extended code: {ExtendedErrorCode} ({(int)ExtendedErrorCode}). " +
        $"Native message: {NativeMessage}";

    public unsafe static void ThrowOnError(SqliteResult result, Sqlite3SafeHandle db, [CallerMemberName] string operation = "")
    {
        if (result == SqliteResult.OK)
            return;

        throw CreateException(result, db, operation);
    }

    public unsafe static SqliteInteropException CreateException(SqliteResult result, Sqlite3SafeHandle db, string operation)
    {
        int extendedCodeValue;
        string nativeMessage;

        if (db.IsInvalid)
        {
            // Read extended code exactly once from the native connection.
            extendedCodeValue = VTableCache.Instance.Db.extended_errcode(db.AsStructPointer());
            byte* pErr = VTableCache.Instance.Db.errmsg(db.AsStructPointer());
            nativeMessage = Marshal.PtrToStringUTF8((nint)pErr) ?? "Unreadable SQLite error";
        }
        else
        {
            extendedCodeValue = (int)result;
            nativeMessage = "Unknown SQLite error";
        }

        SqliteResult baseCode = (SqliteResult)(extendedCodeValue & 0xFF);
        SqliteExtendedErrorCode extendedCode = (SqliteExtendedErrorCode)extendedCodeValue;

        return new SqliteInteropException(
            // $"{operation} failed. SQLite base code: {baseCode}, extended code: {extendedCode} ({extendedCodeValue}) - {extendedErrorDescription}. Native message: {nativeMessage}",
            // $"{operation} failed. SQLite base code: {baseCode}, extended code: {extendedCode} ({extendedCodeValue}) - . Native message: {nativeMessage}",
            baseCode,
            extendedCode,
            nativeMessage,
            operation);
    }
}
