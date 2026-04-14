// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using CiccioSoft.Data.Sqlite.Interop.Native;

namespace CiccioSoft.Data.Sqlite.Interop;

[Flags]
public enum SqlitePrepareFlags : uint
{
    None = 0,
    Persistent = NativeSqlite3.SQLITE_PREPARE_PERSISTENT,
    Normalize = NativeSqlite3.SQLITE_PREPARE_NORMALIZE,
    NoVtab = NativeSqlite3.SQLITE_PREPARE_NO_VTAB,
    DontLog = NativeSqlite3.SQLITE_PREPARE_DONT_LOG
}
