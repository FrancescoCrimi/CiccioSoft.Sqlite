// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using CiccioSoft.Data.Sqlite.Interop.Native;

namespace CiccioSoft.Data.Sqlite.Interop;

public enum SqliteType
{
    Integer = Sqlite3Native.SQLITE_INTEGER,
    Float = Sqlite3Native.SQLITE_FLOAT,
    Text = Sqlite3Native.SQLITE_TEXT,
    Blob = Sqlite3Native.SQLITE_BLOB,
    Null = Sqlite3Native.SQLITE_NULL
}
