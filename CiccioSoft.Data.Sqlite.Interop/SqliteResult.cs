// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using CiccioSoft.Data.Sqlite.Interop.Native;

namespace CiccioSoft.Data.Sqlite.Interop;

public enum SqliteResult
{
    OK = NativeSqlite3.SQLITE_OK,
    Error = NativeSqlite3.SQLITE_ERROR,
    Internal = NativeSqlite3.SQLITE_INTERNAL,
    Perm = NativeSqlite3.SQLITE_PERM,
    Abort = NativeSqlite3.SQLITE_ABORT,
    Busy = NativeSqlite3.SQLITE_BUSY,
    Locked = NativeSqlite3.SQLITE_LOCKED,
    NoMem = NativeSqlite3.SQLITE_NOMEM,
    ReadOnly = NativeSqlite3.SQLITE_READONLY,
    Interrupt = NativeSqlite3.SQLITE_INTERRUPT,
    IOErr = NativeSqlite3.SQLITE_IOERR,
    Corrupt = NativeSqlite3.SQLITE_CORRUPT,
    NotFound = NativeSqlite3.SQLITE_NOTFOUND,
    Full = NativeSqlite3.SQLITE_FULL,
    CantOpen = NativeSqlite3.SQLITE_CANTOPEN,
    Protocol = NativeSqlite3.SQLITE_PROTOCOL,
    Empty = NativeSqlite3.SQLITE_EMPTY,
    Schema = NativeSqlite3.SQLITE_SCHEMA,
    TooBig = NativeSqlite3.SQLITE_TOOBIG,
    Constraint = NativeSqlite3.SQLITE_CONSTRAINT,
    Mismatch = NativeSqlite3.SQLITE_MISMATCH,
    Misuse = NativeSqlite3.SQLITE_MISUSE,
    NoLfs = NativeSqlite3.SQLITE_NOLFS,
    Auth = NativeSqlite3.SQLITE_AUTH,
    Format = NativeSqlite3.SQLITE_FORMAT,
    Range = NativeSqlite3.SQLITE_RANGE,
    NotADb = NativeSqlite3.SQLITE_NOTADB,
    Notice = NativeSqlite3.SQLITE_NOTICE,
    Warning = NativeSqlite3.SQLITE_WARNING,
    Row = NativeSqlite3.SQLITE_ROW,
    Done = NativeSqlite3.SQLITE_DONE
}
