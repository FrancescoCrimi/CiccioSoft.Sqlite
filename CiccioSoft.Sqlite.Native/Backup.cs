// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Runtime.InteropServices;
using CiccioSoft.Sqlite.Native.Interop;

namespace CiccioSoft.Sqlite.Native;

public sealed unsafe class Backup : SafeHandle
{

    #region Ctor and safehandle

    internal Backup(sqlite3_backup* sqlite3_backup)
        : base((nint)sqlite3_backup, true)
    {
    }

    public override bool IsInvalid => handle == nint.Zero;

    protected override bool ReleaseHandle()
    {
        _ = NativeMethods.sqlite3_backup_finish((sqlite3_backup*)handle);
        return true;
    }

    private sqlite3_backup* Sqlite3BackupHandle => (sqlite3_backup*)DangerousGetHandle();

    #endregion


    #region Backup

    public ResultCode Step(int pages = -1)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        ResultCode rtn = (ResultCode)NativeMethods.sqlite3_backup_step(Sqlite3BackupHandle, pages);
        return rtn;
    }

    public int Remaining()
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        int rtn = NativeMethods.sqlite3_backup_remaining(Sqlite3BackupHandle);
        return rtn;
    }

    public int PageCount()
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        int rtn = NativeMethods.sqlite3_backup_pagecount(Sqlite3BackupHandle);
        return rtn;
    }

    public void Finish()
    {
        Dispose();
    }

    #endregion
}
