// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using CiccioSoft.Sqlite.Interop.Native;

namespace CiccioSoft.Sqlite.Interop.Com;

[Flags]
public enum SqlitePrepareFlags : uint
{
    None = 0,
    Persistent = Sqlite3Native.SQLITE_PREPARE_PERSISTENT,
    Normalize = Sqlite3Native.SQLITE_PREPARE_NORMALIZE,
    NoVtab = Sqlite3Native.SQLITE_PREPARE_NO_VTAB,
    DontLog = Sqlite3Native.SQLITE_PREPARE_DONT_LOG
}
