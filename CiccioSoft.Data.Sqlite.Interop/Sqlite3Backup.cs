// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using Microsoft.Win32.SafeHandles;
using CiccioSoft.Data.Sqlite.Interop.Native;

namespace CiccioSoft.Data.Sqlite.Interop;

public sealed class Sqlite3BackupHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    internal Sqlite3BackupHandle(nint handle) : base(true)
    {
        SetHandle(handle);
    }
    protected override bool ReleaseHandle()
    {
        NativeSqlite3.sqlite3_backup_finish(handle);
        return true;
    }
}

public sealed unsafe class Sqlite3Backup : IDisposable
{
    private readonly Sqlite3BackupHandle _handle;

    internal Sqlite3Backup(Sqlite3BackupHandle handle)
    {
        _handle = handle;
    }

    public SqliteResult Step(int pages = -1)
    {
        ThrowIfInvalid();
        return (SqliteResult)NativeSqlite3.sqlite3_backup_step(_handle.DangerousGetHandle(), pages);
    }

    public int Remaining()
    {
        ThrowIfInvalid();
        return NativeSqlite3.sqlite3_backup_remaining(_handle.DangerousGetHandle());
    }

    public int PageCount()
    {
        ThrowIfInvalid();
        return NativeSqlite3.sqlite3_backup_pagecount(_handle.DangerousGetHandle());
    }

    public SqliteResult Finish()
    {
        if (_handle.IsClosed || _handle.IsInvalid)
        {
            return SqliteResult.OK;
        }

        SqliteResult result = (SqliteResult)NativeSqlite3.sqlite3_backup_finish(_handle.DangerousGetHandle());
        _handle.SetHandleAsInvalid();
        _handle.Dispose();
        return result;
    }

    private void ThrowIfInvalid()
    {
        if (_handle.IsClosed || _handle.IsInvalid)
            throw new ObjectDisposedException(nameof(Sqlite3Backup));
    }

    public void Dispose() => _handle.Dispose();
}
