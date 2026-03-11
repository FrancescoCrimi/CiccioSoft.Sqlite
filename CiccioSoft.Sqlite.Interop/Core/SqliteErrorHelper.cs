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
        int extendedCode = (int)result;
        string nativeMessage = "Unknown SQLite error";

        if (db != nint.Zero)
        {
            extendedCode = sqlite3.sqlite3_extended_errcode(db);
            byte* pErr = sqlite3.sqlite3_errmsg(db);
            nativeMessage = Marshal.PtrToStringUTF8((nint)pErr) ?? "Unreadable SQLite error";
        }

        SqliteResult baseCode = (SqliteResult)(extendedCode & 0xFF);

        return new SqliteInteropException(
            $"{operation} failed. SQLite base code: {baseCode}, extended code: {extendedCode}. Native message: {nativeMessage}",
            baseCode,
            extendedCode,
            nativeMessage);
    }
}
