// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using CiccioSoft.Sqlite.Interop.Native;

namespace CiccioSoft.Sqlite.Interop.Light;

[Flags]
public enum SqliteOpenFlags
{
    ReadOnly = Sqlite3Native.SQLITE_OPEN_READONLY,
    ReadWrite = Sqlite3Native.SQLITE_OPEN_READWRITE,
    Create = Sqlite3Native.SQLITE_OPEN_CREATE,
    DELETEONCLOSE = Sqlite3Native.SQLITE_OPEN_DELETEONCLOSE,
    EXCLUSIVE = Sqlite3Native.SQLITE_OPEN_EXCLUSIVE,
    AUTOPROXY = Sqlite3Native.SQLITE_OPEN_AUTOPROXY,
    Uri = Sqlite3Native.SQLITE_OPEN_URI,
    Memory = Sqlite3Native.SQLITE_OPEN_MEMORY,
    MAIN_DB = Sqlite3Native.SQLITE_OPEN_MAIN_DB,
    TEMP_DB = Sqlite3Native.SQLITE_OPEN_TEMP_DB,
    TRANSIENT_DB = Sqlite3Native.SQLITE_OPEN_TRANSIENT_DB,
    MAIN_JOURNAL = Sqlite3Native.SQLITE_OPEN_MAIN_JOURNAL,
    TEMP_JOURNAL = Sqlite3Native.SQLITE_OPEN_TEMP_JOURNAL,
    SUBJOURNAL = Sqlite3Native.SQLITE_OPEN_SUBJOURNAL,
    SUPER_JOURNAL = Sqlite3Native.SQLITE_OPEN_SUPER_JOURNAL,
    NoMutex = Sqlite3Native.SQLITE_OPEN_NOMUTEX,
    FullMutex = Sqlite3Native.SQLITE_OPEN_FULLMUTEX,
    SharedCache = Sqlite3Native.SQLITE_OPEN_SHAREDCACHE,
    PrivateCache = Sqlite3Native.SQLITE_OPEN_PRIVATECACHE,
    WAL = Sqlite3Native.SQLITE_OPEN_WAL,
    NOFOLLOW = Sqlite3Native.SQLITE_OPEN_NOFOLLOW,
    EXRESCODE = Sqlite3Native.SQLITE_OPEN_EXRESCODE,
    MASTER_JOURNAL = Sqlite3Native.SQLITE_OPEN_MASTER_JOURNAL
}
