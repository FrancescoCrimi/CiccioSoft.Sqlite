// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using CiccioSoft.Sqlite.Interop.Native;
using Microsoft.Win32.SafeHandles;

namespace CiccioSoft.Sqlite.Interop.Handles;

public sealed class Sqlite3StmtHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    // public Sqlite3StmtHandle() : base(true) { }
    internal Sqlite3StmtHandle(nint handle) : base(true)
    {
        SetHandle(handle);
    }
    protected override bool ReleaseHandle() =>
        sqlite3.sqlite3_finalize(handle) == sqlite3.SQLITE_OK;
}