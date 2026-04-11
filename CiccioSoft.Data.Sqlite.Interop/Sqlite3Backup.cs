// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using Microsoft.Win32.SafeHandles;

namespace CiccioSoft.Data.Sqlite.Interop;

public sealed class Sqlite3BackupHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    internal Sqlite3BackupHandle(nint handle) : base(true)
    {
        SetHandle(handle);
    }
    protected override bool ReleaseHandle()
    {
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

    public void Dispose() => _handle.Dispose();
}
