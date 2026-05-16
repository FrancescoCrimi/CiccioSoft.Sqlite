// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Runtime.InteropServices;
using CiccioSoft.Sqlite.Interop.Native;

namespace CiccioSoft.Sqlite.Interop;

internal static unsafe class SqliteErrorHelper
{
    public static void ThrowOnError(SqliteResult result, nint db, string operation)
    {
        if (result == SqliteResult.OK)
        {
            return;
        }

        throw CreateException(result, db, operation);
    }

    public static SqliteInteropException CreateException(SqliteResult result, nint db, string operation)
    {
        int extendedCodeValue;
        string nativeMessage;

        if (db != nint.Zero)
        {
            // Read extended code exactly once from the native connection.
            extendedCodeValue = Sqlite3Native.sqlite3_extended_errcode(db);
            byte* pErr = Sqlite3Native.sqlite3_errmsg(db);
            nativeMessage = Marshal.PtrToStringUTF8((nint)pErr) ?? "Unreadable SQLite error";
        }
        else
        {
            extendedCodeValue = (int)result;
            nativeMessage = "Unknown SQLite error";
        }

        SqliteResult baseCode = (SqliteResult)(extendedCodeValue & 0xFF);
        SqliteExtendedErrorCode extendedCode = (SqliteExtendedErrorCode)extendedCodeValue;
        string extendedErrorDescription = GetExtendedErrorDescription(extendedCode);

        return new SqliteInteropException(
            $"{operation} failed. SQLite base code: {baseCode}, extended code: {extendedCode} ({extendedCodeValue}) - {extendedErrorDescription}. Native message: {nativeMessage}",
            baseCode,
            extendedCode,
            nativeMessage);
    }

    public static string GetExtendedErrorDescription(SqliteExtendedErrorCode extendedCode)
    {
        return Enum.IsDefined(typeof(SqliteExtendedErrorCode), extendedCode)
            ? extendedCode.ToString()
            : $"Unknown extended SQLite error code ({(int)extendedCode})";
    }
}
