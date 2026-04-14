// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using CiccioSoft.Data.Sqlite.Interop.Native;

namespace CiccioSoft.Data.Sqlite.Interop;

[Flags]
public enum SqliteOpenFlags
{
    ReadOnly = NativeSqlite3.SQLITE_OPEN_READONLY,
    ReadWrite = NativeSqlite3.SQLITE_OPEN_READWRITE,
    Create = NativeSqlite3.SQLITE_OPEN_CREATE,
    DELETEONCLOSE = NativeSqlite3.SQLITE_OPEN_DELETEONCLOSE,
    EXCLUSIVE = NativeSqlite3.SQLITE_OPEN_EXCLUSIVE,
    AUTOPROXY = NativeSqlite3.SQLITE_OPEN_AUTOPROXY,
    Uri = NativeSqlite3.SQLITE_OPEN_URI,
    Memory = NativeSqlite3.SQLITE_OPEN_MEMORY,
    MAIN_DB = NativeSqlite3.SQLITE_OPEN_MAIN_DB,
    TEMP_DB = NativeSqlite3.SQLITE_OPEN_TEMP_DB,
    TRANSIENT_DB = NativeSqlite3.SQLITE_OPEN_TRANSIENT_DB,
    MAIN_JOURNAL = NativeSqlite3.SQLITE_OPEN_MAIN_JOURNAL,
    TEMP_JOURNAL = NativeSqlite3.SQLITE_OPEN_TEMP_JOURNAL,
    SUBJOURNAL = NativeSqlite3.SQLITE_OPEN_SUBJOURNAL,
    SUPER_JOURNAL = NativeSqlite3.SQLITE_OPEN_SUPER_JOURNAL,
    NoMutex = NativeSqlite3.SQLITE_OPEN_NOMUTEX,
    FullMutex = NativeSqlite3.SQLITE_OPEN_FULLMUTEX,
    SharedCache = NativeSqlite3.SQLITE_OPEN_SHAREDCACHE,
    PrivateCache = NativeSqlite3.SQLITE_OPEN_PRIVATECACHE,
    WAL = NativeSqlite3.SQLITE_OPEN_WAL,
    NOFOLLOW = NativeSqlite3.SQLITE_OPEN_NOFOLLOW,
    EXRESCODE = NativeSqlite3.SQLITE_OPEN_EXRESCODE,
    MASTER_JOURNAL = NativeSqlite3.SQLITE_OPEN_MASTER_JOURNAL
}
