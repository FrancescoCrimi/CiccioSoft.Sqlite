// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using CiccioSoft.Sqlite.Interop.Native;

namespace CiccioSoft.Sqlite.Interop;

[Flags]
public enum SqliteOpenFlags
{
    ReadOnly = Sqlite3Native.SQLITE_OPEN_READONLY,
    ReadWrite = Sqlite3Native.SQLITE_OPEN_READWRITE,
    Create = Sqlite3Native.SQLITE_OPEN_CREATE,
    DeleteOnClose = Sqlite3Native.SQLITE_OPEN_DELETEONCLOSE,
    Exclusive = Sqlite3Native.SQLITE_OPEN_EXCLUSIVE,
    AutoProxy = Sqlite3Native.SQLITE_OPEN_AUTOPROXY,
    Uri = Sqlite3Native.SQLITE_OPEN_URI,
    Memory = Sqlite3Native.SQLITE_OPEN_MEMORY,
    MainDb = Sqlite3Native.SQLITE_OPEN_MAIN_DB,
    TempDb = Sqlite3Native.SQLITE_OPEN_TEMP_DB,
    TransientDb = Sqlite3Native.SQLITE_OPEN_TRANSIENT_DB,
    MainJournal = Sqlite3Native.SQLITE_OPEN_MAIN_JOURNAL,
    TempJournal = Sqlite3Native.SQLITE_OPEN_TEMP_JOURNAL,
    Subjournal = Sqlite3Native.SQLITE_OPEN_SUBJOURNAL,
    SuperJournal = Sqlite3Native.SQLITE_OPEN_SUPER_JOURNAL,
    NoMutex = Sqlite3Native.SQLITE_OPEN_NOMUTEX,
    FullMutex = Sqlite3Native.SQLITE_OPEN_FULLMUTEX,
    SharedCache = Sqlite3Native.SQLITE_OPEN_SHAREDCACHE,
    PrivateCache = Sqlite3Native.SQLITE_OPEN_PRIVATECACHE,
    Wal = Sqlite3Native.SQLITE_OPEN_WAL,
    Nofollow = Sqlite3Native.SQLITE_OPEN_NOFOLLOW,
    Exrescode = Sqlite3Native.SQLITE_OPEN_EXRESCODE,
    MasterJournal = Sqlite3Native.SQLITE_OPEN_MASTER_JOURNAL
}
