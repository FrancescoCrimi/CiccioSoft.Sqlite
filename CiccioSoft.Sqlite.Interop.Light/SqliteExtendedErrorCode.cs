// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using CiccioSoft.Sqlite.Interop.NativeNew;

namespace CiccioSoft.Sqlite.Interop.Light;

/// <summary>
/// Represents SQLite extended error codes.
/// </summary>
public enum SqliteExtendedErrorCode
{
    ErrorMissingCollSeq = Sqlite3Native.SQLITE_ERROR_MISSING_COLLSEQ,
    ErrorRetry = Sqlite3Native.SQLITE_ERROR_RETRY,
    ErrorSnapshot = Sqlite3Native.SQLITE_ERROR_SNAPSHOT,

    IOErrRead = Sqlite3Native.SQLITE_IOERR_READ,
    IOErrShortRead = Sqlite3Native.SQLITE_IOERR_SHORT_READ,
    IOErrWrite = Sqlite3Native.SQLITE_IOERR_WRITE,
    IOErrFsync = Sqlite3Native.SQLITE_IOERR_FSYNC,
    IOErrDirFsync = Sqlite3Native.SQLITE_IOERR_DIR_FSYNC,
    IOErrTruncate = Sqlite3Native.SQLITE_IOERR_TRUNCATE,
    IOErrFstat = Sqlite3Native.SQLITE_IOERR_FSTAT,
    IOErrUnlock = Sqlite3Native.SQLITE_IOERR_UNLOCK,
    IOErrRdlock = Sqlite3Native.SQLITE_IOERR_RDLOCK,
    IOErrDelete = Sqlite3Native.SQLITE_IOERR_DELETE,
    IOErrBlocked = Sqlite3Native.SQLITE_IOERR_BLOCKED,
    IOErrNoMem = Sqlite3Native.SQLITE_IOERR_NOMEM,
    IOErrAccess = Sqlite3Native.SQLITE_IOERR_ACCESS,
    IOErrCheckReservedLock = Sqlite3Native.SQLITE_IOERR_CHECKRESERVEDLOCK,
    IOErrLock = Sqlite3Native.SQLITE_IOERR_LOCK,
    IOErrClose = Sqlite3Native.SQLITE_IOERR_CLOSE,
    IOErrDirClose = Sqlite3Native.SQLITE_IOERR_DIR_CLOSE,
    IOErrShmOpen = Sqlite3Native.SQLITE_IOERR_SHMOPEN,
    IOErrShmSize = Sqlite3Native.SQLITE_IOERR_SHMSIZE,
    IOErrShmLock = Sqlite3Native.SQLITE_IOERR_SHMLOCK,
    IOErrShmMap = Sqlite3Native.SQLITE_IOERR_SHMMAP,
    IOErrSeek = Sqlite3Native.SQLITE_IOERR_SEEK,
    IOErrDeleteNoEnt = Sqlite3Native.SQLITE_IOERR_DELETE_NOENT,
    IOErrMmap = Sqlite3Native.SQLITE_IOERR_MMAP,
    IOErrGetTempPath = Sqlite3Native.SQLITE_IOERR_GETTEMPPATH,
    IOErrConvPath = Sqlite3Native.SQLITE_IOERR_CONVPATH,
    IOErrVnode = Sqlite3Native.SQLITE_IOERR_VNODE,
    IOErrAuth = Sqlite3Native.SQLITE_IOERR_AUTH,
    IOErrBeginAtomic = Sqlite3Native.SQLITE_IOERR_BEGIN_ATOMIC,
    IOErrCommitAtomic = Sqlite3Native.SQLITE_IOERR_COMMIT_ATOMIC,
    IOErrRollbackAtomic = Sqlite3Native.SQLITE_IOERR_ROLLBACK_ATOMIC,
    IOErrData = Sqlite3Native.SQLITE_IOERR_DATA,
    IOErrCorruptFs = Sqlite3Native.SQLITE_IOERR_CORRUPTFS,
    IOErrInPage = Sqlite3Native.SQLITE_IOERR_IN_PAGE,

    LockedSharedCache = Sqlite3Native.SQLITE_LOCKED_SHAREDCACHE,
    LockedVtab = Sqlite3Native.SQLITE_LOCKED_VTAB,

    BusyRecovery = Sqlite3Native.SQLITE_BUSY_RECOVERY,
    BusySnapshot = Sqlite3Native.SQLITE_BUSY_SNAPSHOT,
    BusyTimeout = Sqlite3Native.SQLITE_BUSY_TIMEOUT,

    CantOpenNoTempDir = Sqlite3Native.SQLITE_CANTOPEN_NOTEMPDIR,
    CantOpenIsDir = Sqlite3Native.SQLITE_CANTOPEN_ISDIR,
    CantOpenFullPath = Sqlite3Native.SQLITE_CANTOPEN_FULLPATH,
    CantOpenConvPath = Sqlite3Native.SQLITE_CANTOPEN_CONVPATH,
    CantOpenDirtyWal = Sqlite3Native.SQLITE_CANTOPEN_DIRTYWAL,
    CantOpenSymlink = Sqlite3Native.SQLITE_CANTOPEN_SYMLINK,

    CorruptVtab = Sqlite3Native.SQLITE_CORRUPT_VTAB,
    CorruptSequence = Sqlite3Native.SQLITE_CORRUPT_SEQUENCE,
    CorruptIndex = Sqlite3Native.SQLITE_CORRUPT_INDEX,

    ReadOnlyRecovery = Sqlite3Native.SQLITE_READONLY_RECOVERY,
    ReadOnlyCantLock = Sqlite3Native.SQLITE_READONLY_CANTLOCK,
    ReadOnlyRollback = Sqlite3Native.SQLITE_READONLY_ROLLBACK,
    ReadOnlyDbMoved = Sqlite3Native.SQLITE_READONLY_DBMOVED,
    ReadOnlyCantInit = Sqlite3Native.SQLITE_READONLY_CANTINIT,
    ReadOnlyDirectory = Sqlite3Native.SQLITE_READONLY_DIRECTORY,

    AbortRollback = Sqlite3Native.SQLITE_ABORT_ROLLBACK,

    ConstraintCheck = Sqlite3Native.SQLITE_CONSTRAINT_CHECK,
    ConstraintCommitHook = Sqlite3Native.SQLITE_CONSTRAINT_COMMITHOOK,
    ConstraintForeignKey = Sqlite3Native.SQLITE_CONSTRAINT_FOREIGNKEY,
    ConstraintFunction = Sqlite3Native.SQLITE_CONSTRAINT_FUNCTION,
    ConstraintNotNull = Sqlite3Native.SQLITE_CONSTRAINT_NOTNULL,
    ConstraintPrimaryKey = Sqlite3Native.SQLITE_CONSTRAINT_PRIMARYKEY,
    ConstraintTrigger = Sqlite3Native.SQLITE_CONSTRAINT_TRIGGER,
    ConstraintUnique = Sqlite3Native.SQLITE_CONSTRAINT_UNIQUE,
    ConstraintVtab = Sqlite3Native.SQLITE_CONSTRAINT_VTAB,
    ConstraintRowId = Sqlite3Native.SQLITE_CONSTRAINT_ROWID,
    ConstraintPinned = Sqlite3Native.SQLITE_CONSTRAINT_PINNED,
    ConstraintDataType = Sqlite3Native.SQLITE_CONSTRAINT_DATATYPE,

    NoticeRecoverWal = Sqlite3Native.SQLITE_NOTICE_RECOVER_WAL,
    NoticeRecoverRollback = Sqlite3Native.SQLITE_NOTICE_RECOVER_ROLLBACK,
    NoticeRbu = Sqlite3Native.SQLITE_NOTICE_RBU,

    WarningAutoIndex = Sqlite3Native.SQLITE_WARNING_AUTOINDEX,

    AuthUser = Sqlite3Native.SQLITE_AUTH_USER,

    OkLoadPermanently = Sqlite3Native.SQLITE_OK_LOAD_PERMANENTLY,
    OkSymlink = Sqlite3Native.SQLITE_OK_SYMLINK
}
