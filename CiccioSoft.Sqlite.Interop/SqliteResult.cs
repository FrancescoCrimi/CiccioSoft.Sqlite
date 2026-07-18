// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using CiccioSoft.Sqlite.Interop.Native;

namespace CiccioSoft.Sqlite.Interop;

public enum SqliteResult
{
    OK          = Sqlite3Native.SQLITE_OK,
    Error       = Sqlite3Native.SQLITE_ERROR,
    Internal    = Sqlite3Native.SQLITE_INTERNAL,
    Perm        = Sqlite3Native.SQLITE_PERM,
    Abort       = Sqlite3Native.SQLITE_ABORT,
    Busy        = Sqlite3Native.SQLITE_BUSY,
    Locked      = Sqlite3Native.SQLITE_LOCKED,
    NoMem       = Sqlite3Native.SQLITE_NOMEM,
    ReadOnly    = Sqlite3Native.SQLITE_READONLY,
    Interrupt   = Sqlite3Native.SQLITE_INTERRUPT,
    IOErr       = Sqlite3Native.SQLITE_IOERR,
    Corrupt     = Sqlite3Native.SQLITE_CORRUPT,
    NotFound    = Sqlite3Native.SQLITE_NOTFOUND,
    Full        = Sqlite3Native.SQLITE_FULL,
    CantOpen    = Sqlite3Native.SQLITE_CANTOPEN,
    Protocol    = Sqlite3Native.SQLITE_PROTOCOL,
    Empty       = Sqlite3Native.SQLITE_EMPTY,
    Schema      = Sqlite3Native.SQLITE_SCHEMA,
    TooBig      = Sqlite3Native.SQLITE_TOOBIG,
    Constraint  = Sqlite3Native.SQLITE_CONSTRAINT,
    Mismatch    = Sqlite3Native.SQLITE_MISMATCH,
    Misuse      = Sqlite3Native.SQLITE_MISUSE,
    NoLfs       = Sqlite3Native.SQLITE_NOLFS,
    Auth        = Sqlite3Native.SQLITE_AUTH,
    Format      = Sqlite3Native.SQLITE_FORMAT,
    Range       = Sqlite3Native.SQLITE_RANGE,
    NotADb      = Sqlite3Native.SQLITE_NOTADB,
    Notice      = Sqlite3Native.SQLITE_NOTICE,
    Warning     = Sqlite3Native.SQLITE_WARNING,
    Row         = Sqlite3Native.SQLITE_ROW,
    Done        = Sqlite3Native.SQLITE_DONE
}
