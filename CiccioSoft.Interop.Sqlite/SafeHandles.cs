// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Runtime.InteropServices;

namespace CiccioSoft.Interop.Sqlite;

public sealed unsafe class SafeConnectionHandle : SafeHandle
{
    internal SafeConnectionHandle(sqlite3* sqlite3)
        : base((nint)sqlite3, true)
    {
    }

    public override bool IsInvalid => handle == nint.Zero;

    internal sqlite3* AsStructPointer() => (sqlite3*)handle;

    protected override bool ReleaseHandle()
    {
        return NativeMethods.sqlite3_close_v2((sqlite3*)handle) == NativeMethods.SQLITE_OK;
    }
}

public sealed unsafe class SafeStatementHandle : SafeHandle
{
    internal SafeStatementHandle(sqlite3_stmt* pStmt)
        : base((nint)pStmt, true)
    {
    }

    public override bool IsInvalid => handle == nint.Zero;

    internal sqlite3_stmt* AsStructPointer() => (sqlite3_stmt*)handle;

    protected override bool ReleaseHandle()
    {
        return NativeMethods.sqlite3_finalize((sqlite3_stmt*)handle) == NativeMethods.SQLITE_OK;
    }
}


public sealed unsafe class SafeBackupHandle : SafeHandle
{
    internal SafeBackupHandle(sqlite3_backup* sqlite3_backup)
        : base((nint)sqlite3_backup, true)
    {
    }

    public override bool IsInvalid => handle == nint.Zero;

    internal sqlite3_backup* AsStructPointer() => (sqlite3_backup*)handle;

    protected override bool ReleaseHandle()
    {
        return NativeMethods.sqlite3_backup_finish((sqlite3_backup*)handle) == NativeMethods.SQLITE_OK;
    }
}

public sealed unsafe class SafeBlobHandle : SafeHandle
{
    internal SafeBlobHandle(sqlite3_blob* pBlob)
        : base((nint)pBlob, true)
    {
    }

    public override bool IsInvalid => handle == nint.Zero;

    internal sqlite3_blob* AsStructPointer() => (sqlite3_blob*)handle;

    protected override bool ReleaseHandle()
    {
        return NativeMethods.sqlite3_blob_close((sqlite3_blob*)handle) == NativeMethods.SQLITE_OK;
    }
}
