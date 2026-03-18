// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using CiccioSoft.Sqlite.Interop.Native;
using Microsoft.Win32.SafeHandles;

namespace CiccioSoft.Sqlite.Interop.Handles;

public sealed class Sqlite3Handle : SafeHandleZeroOrMinusOneIsInvalid
{
    // public Sqlite3Handle() : base(true) { }
    internal Sqlite3Handle(nint handle) : base(true)
    {
        SetHandle(handle);
    }
    protected override bool ReleaseHandle()
    {
        // return SqliteNative.sqlite3_close_v2(handle) == SqliteNative.SQLITE_OK;
        NativeSqlite3.sqlite3_close_v2(handle);
        return true;
    }
}