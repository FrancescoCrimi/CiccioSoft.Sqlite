// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using CiccioSoft.Data.Sqlite.Interop.Native;

namespace CiccioSoft.Data.Sqlite.Interop;

public enum SqliteType
{
    Integer = NativeSqlite3.SQLITE_INTEGER,
    Float = NativeSqlite3.SQLITE_FLOAT,
    Text = NativeSqlite3.SQLITE_TEXT,
    Blob = NativeSqlite3.SQLITE_BLOB,
    Null = NativeSqlite3.SQLITE_NULL
}
