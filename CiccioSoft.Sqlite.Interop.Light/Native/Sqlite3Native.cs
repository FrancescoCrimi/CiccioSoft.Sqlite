using System;

namespace CiccioSoft.Sqlite.Interop.Native
{
    internal static partial class Sqlite3Native
    {
        [NativeTypeName("#define SQLITE_VERSION \"3.50.4\"")]
        public static ReadOnlySpan<byte> SQLITE_VERSION => "3.50.4"u8;

        [NativeTypeName("#define SQLITE_VERSION_NUMBER 3050004")]
        public const int SQLITE_VERSION_NUMBER = 3050004;

        [NativeTypeName("#define SQLITE_SOURCE_ID \"2025-07-30 19:33:53 4d8adfb30e03f9cf27f800a2c1ba3c48fb4ca1b08b0f5ed59a4d5ecbf45e20a3\"")]
        public static ReadOnlySpan<byte> SQLITE_SOURCE_ID => "2025-07-30 19:33:53 4d8adfb30e03f9cf27f800a2c1ba3c48fb4ca1b08b0f5ed59a4d5ecbf45e20a3"u8;

        [NativeTypeName("#define SQLITE_OK 0")]
        public const int SQLITE_OK = 0;

        [NativeTypeName("#define SQLITE_ERROR 1")]
        public const int SQLITE_ERROR = 1;

        [NativeTypeName("#define SQLITE_INTERNAL 2")]
        public const int SQLITE_INTERNAL = 2;

        [NativeTypeName("#define SQLITE_PERM 3")]
        public const int SQLITE_PERM = 3;

        [NativeTypeName("#define SQLITE_ABORT 4")]
        public const int SQLITE_ABORT = 4;

        [NativeTypeName("#define SQLITE_BUSY 5")]
        public const int SQLITE_BUSY = 5;

        [NativeTypeName("#define SQLITE_LOCKED 6")]
        public const int SQLITE_LOCKED = 6;

        [NativeTypeName("#define SQLITE_NOMEM 7")]
        public const int SQLITE_NOMEM = 7;

        [NativeTypeName("#define SQLITE_READONLY 8")]
        public const int SQLITE_READONLY = 8;

        [NativeTypeName("#define SQLITE_INTERRUPT 9")]
        public const int SQLITE_INTERRUPT = 9;

        [NativeTypeName("#define SQLITE_IOERR 10")]
        public const int SQLITE_IOERR = 10;

        [NativeTypeName("#define SQLITE_CORRUPT 11")]
        public const int SQLITE_CORRUPT = 11;

        [NativeTypeName("#define SQLITE_NOTFOUND 12")]
        public const int SQLITE_NOTFOUND = 12;

        [NativeTypeName("#define SQLITE_FULL 13")]
        public const int SQLITE_FULL = 13;

        [NativeTypeName("#define SQLITE_CANTOPEN 14")]
        public const int SQLITE_CANTOPEN = 14;

        [NativeTypeName("#define SQLITE_PROTOCOL 15")]
        public const int SQLITE_PROTOCOL = 15;

        [NativeTypeName("#define SQLITE_EMPTY 16")]
        public const int SQLITE_EMPTY = 16;

        [NativeTypeName("#define SQLITE_SCHEMA 17")]
        public const int SQLITE_SCHEMA = 17;

        [NativeTypeName("#define SQLITE_TOOBIG 18")]
        public const int SQLITE_TOOBIG = 18;

        [NativeTypeName("#define SQLITE_CONSTRAINT 19")]
        public const int SQLITE_CONSTRAINT = 19;

        [NativeTypeName("#define SQLITE_MISMATCH 20")]
        public const int SQLITE_MISMATCH = 20;

        [NativeTypeName("#define SQLITE_MISUSE 21")]
        public const int SQLITE_MISUSE = 21;

        [NativeTypeName("#define SQLITE_NOLFS 22")]
        public const int SQLITE_NOLFS = 22;

        [NativeTypeName("#define SQLITE_AUTH 23")]
        public const int SQLITE_AUTH = 23;

        [NativeTypeName("#define SQLITE_FORMAT 24")]
        public const int SQLITE_FORMAT = 24;

        [NativeTypeName("#define SQLITE_RANGE 25")]
        public const int SQLITE_RANGE = 25;

        [NativeTypeName("#define SQLITE_NOTADB 26")]
        public const int SQLITE_NOTADB = 26;

        [NativeTypeName("#define SQLITE_NOTICE 27")]
        public const int SQLITE_NOTICE = 27;

        [NativeTypeName("#define SQLITE_WARNING 28")]
        public const int SQLITE_WARNING = 28;

        [NativeTypeName("#define SQLITE_ROW 100")]
        public const int SQLITE_ROW = 100;

        [NativeTypeName("#define SQLITE_DONE 101")]
        public const int SQLITE_DONE = 101;

        [NativeTypeName("#define SQLITE_ERROR_MISSING_COLLSEQ (SQLITE_ERROR | (1<<8))")]
        public const int SQLITE_ERROR_MISSING_COLLSEQ = (1 | (1 << 8));

        [NativeTypeName("#define SQLITE_ERROR_RETRY (SQLITE_ERROR | (2<<8))")]
        public const int SQLITE_ERROR_RETRY = (1 | (2 << 8));

        [NativeTypeName("#define SQLITE_ERROR_SNAPSHOT (SQLITE_ERROR | (3<<8))")]
        public const int SQLITE_ERROR_SNAPSHOT = (1 | (3 << 8));

        [NativeTypeName("#define SQLITE_IOERR_READ (SQLITE_IOERR | (1<<8))")]
        public const int SQLITE_IOERR_READ = (10 | (1 << 8));

        [NativeTypeName("#define SQLITE_IOERR_SHORT_READ (SQLITE_IOERR | (2<<8))")]
        public const int SQLITE_IOERR_SHORT_READ = (10 | (2 << 8));

        [NativeTypeName("#define SQLITE_IOERR_WRITE (SQLITE_IOERR | (3<<8))")]
        public const int SQLITE_IOERR_WRITE = (10 | (3 << 8));

        [NativeTypeName("#define SQLITE_IOERR_FSYNC (SQLITE_IOERR | (4<<8))")]
        public const int SQLITE_IOERR_FSYNC = (10 | (4 << 8));

        [NativeTypeName("#define SQLITE_IOERR_DIR_FSYNC (SQLITE_IOERR | (5<<8))")]
        public const int SQLITE_IOERR_DIR_FSYNC = (10 | (5 << 8));

        [NativeTypeName("#define SQLITE_IOERR_TRUNCATE (SQLITE_IOERR | (6<<8))")]
        public const int SQLITE_IOERR_TRUNCATE = (10 | (6 << 8));

        [NativeTypeName("#define SQLITE_IOERR_FSTAT (SQLITE_IOERR | (7<<8))")]
        public const int SQLITE_IOERR_FSTAT = (10 | (7 << 8));

        [NativeTypeName("#define SQLITE_IOERR_UNLOCK (SQLITE_IOERR | (8<<8))")]
        public const int SQLITE_IOERR_UNLOCK = (10 | (8 << 8));

        [NativeTypeName("#define SQLITE_IOERR_RDLOCK (SQLITE_IOERR | (9<<8))")]
        public const int SQLITE_IOERR_RDLOCK = (10 | (9 << 8));

        [NativeTypeName("#define SQLITE_IOERR_DELETE (SQLITE_IOERR | (10<<8))")]
        public const int SQLITE_IOERR_DELETE = (10 | (10 << 8));

        [NativeTypeName("#define SQLITE_IOERR_BLOCKED (SQLITE_IOERR | (11<<8))")]
        public const int SQLITE_IOERR_BLOCKED = (10 | (11 << 8));

        [NativeTypeName("#define SQLITE_IOERR_NOMEM (SQLITE_IOERR | (12<<8))")]
        public const int SQLITE_IOERR_NOMEM = (10 | (12 << 8));

        [NativeTypeName("#define SQLITE_IOERR_ACCESS (SQLITE_IOERR | (13<<8))")]
        public const int SQLITE_IOERR_ACCESS = (10 | (13 << 8));

        [NativeTypeName("#define SQLITE_IOERR_CHECKRESERVEDLOCK (SQLITE_IOERR | (14<<8))")]
        public const int SQLITE_IOERR_CHECKRESERVEDLOCK = (10 | (14 << 8));

        [NativeTypeName("#define SQLITE_IOERR_LOCK (SQLITE_IOERR | (15<<8))")]
        public const int SQLITE_IOERR_LOCK = (10 | (15 << 8));

        [NativeTypeName("#define SQLITE_IOERR_CLOSE (SQLITE_IOERR | (16<<8))")]
        public const int SQLITE_IOERR_CLOSE = (10 | (16 << 8));

        [NativeTypeName("#define SQLITE_IOERR_DIR_CLOSE (SQLITE_IOERR | (17<<8))")]
        public const int SQLITE_IOERR_DIR_CLOSE = (10 | (17 << 8));

        [NativeTypeName("#define SQLITE_IOERR_SHMOPEN (SQLITE_IOERR | (18<<8))")]
        public const int SQLITE_IOERR_SHMOPEN = (10 | (18 << 8));

        [NativeTypeName("#define SQLITE_IOERR_SHMSIZE (SQLITE_IOERR | (19<<8))")]
        public const int SQLITE_IOERR_SHMSIZE = (10 | (19 << 8));

        [NativeTypeName("#define SQLITE_IOERR_SHMLOCK (SQLITE_IOERR | (20<<8))")]
        public const int SQLITE_IOERR_SHMLOCK = (10 | (20 << 8));

        [NativeTypeName("#define SQLITE_IOERR_SHMMAP (SQLITE_IOERR | (21<<8))")]
        public const int SQLITE_IOERR_SHMMAP = (10 | (21 << 8));

        [NativeTypeName("#define SQLITE_IOERR_SEEK (SQLITE_IOERR | (22<<8))")]
        public const int SQLITE_IOERR_SEEK = (10 | (22 << 8));

        [NativeTypeName("#define SQLITE_IOERR_DELETE_NOENT (SQLITE_IOERR | (23<<8))")]
        public const int SQLITE_IOERR_DELETE_NOENT = (10 | (23 << 8));

        [NativeTypeName("#define SQLITE_IOERR_MMAP (SQLITE_IOERR | (24<<8))")]
        public const int SQLITE_IOERR_MMAP = (10 | (24 << 8));

        [NativeTypeName("#define SQLITE_IOERR_GETTEMPPATH (SQLITE_IOERR | (25<<8))")]
        public const int SQLITE_IOERR_GETTEMPPATH = (10 | (25 << 8));

        [NativeTypeName("#define SQLITE_IOERR_CONVPATH (SQLITE_IOERR | (26<<8))")]
        public const int SQLITE_IOERR_CONVPATH = (10 | (26 << 8));

        [NativeTypeName("#define SQLITE_IOERR_VNODE (SQLITE_IOERR | (27<<8))")]
        public const int SQLITE_IOERR_VNODE = (10 | (27 << 8));

        [NativeTypeName("#define SQLITE_IOERR_AUTH (SQLITE_IOERR | (28<<8))")]
        public const int SQLITE_IOERR_AUTH = (10 | (28 << 8));

        [NativeTypeName("#define SQLITE_IOERR_BEGIN_ATOMIC (SQLITE_IOERR | (29<<8))")]
        public const int SQLITE_IOERR_BEGIN_ATOMIC = (10 | (29 << 8));

        [NativeTypeName("#define SQLITE_IOERR_COMMIT_ATOMIC (SQLITE_IOERR | (30<<8))")]
        public const int SQLITE_IOERR_COMMIT_ATOMIC = (10 | (30 << 8));

        [NativeTypeName("#define SQLITE_IOERR_ROLLBACK_ATOMIC (SQLITE_IOERR | (31<<8))")]
        public const int SQLITE_IOERR_ROLLBACK_ATOMIC = (10 | (31 << 8));

        [NativeTypeName("#define SQLITE_IOERR_DATA (SQLITE_IOERR | (32<<8))")]
        public const int SQLITE_IOERR_DATA = (10 | (32 << 8));

        [NativeTypeName("#define SQLITE_IOERR_CORRUPTFS (SQLITE_IOERR | (33<<8))")]
        public const int SQLITE_IOERR_CORRUPTFS = (10 | (33 << 8));

        [NativeTypeName("#define SQLITE_IOERR_IN_PAGE (SQLITE_IOERR | (34<<8))")]
        public const int SQLITE_IOERR_IN_PAGE = (10 | (34 << 8));

        [NativeTypeName("#define SQLITE_LOCKED_SHAREDCACHE (SQLITE_LOCKED |  (1<<8))")]
        public const int SQLITE_LOCKED_SHAREDCACHE = (6 | (1 << 8));

        [NativeTypeName("#define SQLITE_LOCKED_VTAB (SQLITE_LOCKED |  (2<<8))")]
        public const int SQLITE_LOCKED_VTAB = (6 | (2 << 8));

        [NativeTypeName("#define SQLITE_BUSY_RECOVERY (SQLITE_BUSY   |  (1<<8))")]
        public const int SQLITE_BUSY_RECOVERY = (5 | (1 << 8));

        [NativeTypeName("#define SQLITE_BUSY_SNAPSHOT (SQLITE_BUSY   |  (2<<8))")]
        public const int SQLITE_BUSY_SNAPSHOT = (5 | (2 << 8));

        [NativeTypeName("#define SQLITE_BUSY_TIMEOUT (SQLITE_BUSY   |  (3<<8))")]
        public const int SQLITE_BUSY_TIMEOUT = (5 | (3 << 8));

        [NativeTypeName("#define SQLITE_CANTOPEN_NOTEMPDIR (SQLITE_CANTOPEN | (1<<8))")]
        public const int SQLITE_CANTOPEN_NOTEMPDIR = (14 | (1 << 8));

        [NativeTypeName("#define SQLITE_CANTOPEN_ISDIR (SQLITE_CANTOPEN | (2<<8))")]
        public const int SQLITE_CANTOPEN_ISDIR = (14 | (2 << 8));

        [NativeTypeName("#define SQLITE_CANTOPEN_FULLPATH (SQLITE_CANTOPEN | (3<<8))")]
        public const int SQLITE_CANTOPEN_FULLPATH = (14 | (3 << 8));

        [NativeTypeName("#define SQLITE_CANTOPEN_CONVPATH (SQLITE_CANTOPEN | (4<<8))")]
        public const int SQLITE_CANTOPEN_CONVPATH = (14 | (4 << 8));

        [NativeTypeName("#define SQLITE_CANTOPEN_DIRTYWAL (SQLITE_CANTOPEN | (5<<8))")]
        public const int SQLITE_CANTOPEN_DIRTYWAL = (14 | (5 << 8));

        [NativeTypeName("#define SQLITE_CANTOPEN_SYMLINK (SQLITE_CANTOPEN | (6<<8))")]
        public const int SQLITE_CANTOPEN_SYMLINK = (14 | (6 << 8));

        [NativeTypeName("#define SQLITE_CORRUPT_VTAB (SQLITE_CORRUPT | (1<<8))")]
        public const int SQLITE_CORRUPT_VTAB = (11 | (1 << 8));

        [NativeTypeName("#define SQLITE_CORRUPT_SEQUENCE (SQLITE_CORRUPT | (2<<8))")]
        public const int SQLITE_CORRUPT_SEQUENCE = (11 | (2 << 8));

        [NativeTypeName("#define SQLITE_CORRUPT_INDEX (SQLITE_CORRUPT | (3<<8))")]
        public const int SQLITE_CORRUPT_INDEX = (11 | (3 << 8));

        [NativeTypeName("#define SQLITE_READONLY_RECOVERY (SQLITE_READONLY | (1<<8))")]
        public const int SQLITE_READONLY_RECOVERY = (8 | (1 << 8));

        [NativeTypeName("#define SQLITE_READONLY_CANTLOCK (SQLITE_READONLY | (2<<8))")]
        public const int SQLITE_READONLY_CANTLOCK = (8 | (2 << 8));

        [NativeTypeName("#define SQLITE_READONLY_ROLLBACK (SQLITE_READONLY | (3<<8))")]
        public const int SQLITE_READONLY_ROLLBACK = (8 | (3 << 8));

        [NativeTypeName("#define SQLITE_READONLY_DBMOVED (SQLITE_READONLY | (4<<8))")]
        public const int SQLITE_READONLY_DBMOVED = (8 | (4 << 8));

        [NativeTypeName("#define SQLITE_READONLY_CANTINIT (SQLITE_READONLY | (5<<8))")]
        public const int SQLITE_READONLY_CANTINIT = (8 | (5 << 8));

        [NativeTypeName("#define SQLITE_READONLY_DIRECTORY (SQLITE_READONLY | (6<<8))")]
        public const int SQLITE_READONLY_DIRECTORY = (8 | (6 << 8));

        [NativeTypeName("#define SQLITE_ABORT_ROLLBACK (SQLITE_ABORT | (2<<8))")]
        public const int SQLITE_ABORT_ROLLBACK = (4 | (2 << 8));

        [NativeTypeName("#define SQLITE_CONSTRAINT_CHECK (SQLITE_CONSTRAINT | (1<<8))")]
        public const int SQLITE_CONSTRAINT_CHECK = (19 | (1 << 8));

        [NativeTypeName("#define SQLITE_CONSTRAINT_COMMITHOOK (SQLITE_CONSTRAINT | (2<<8))")]
        public const int SQLITE_CONSTRAINT_COMMITHOOK = (19 | (2 << 8));

        [NativeTypeName("#define SQLITE_CONSTRAINT_FOREIGNKEY (SQLITE_CONSTRAINT | (3<<8))")]
        public const int SQLITE_CONSTRAINT_FOREIGNKEY = (19 | (3 << 8));

        [NativeTypeName("#define SQLITE_CONSTRAINT_FUNCTION (SQLITE_CONSTRAINT | (4<<8))")]
        public const int SQLITE_CONSTRAINT_FUNCTION = (19 | (4 << 8));

        [NativeTypeName("#define SQLITE_CONSTRAINT_NOTNULL (SQLITE_CONSTRAINT | (5<<8))")]
        public const int SQLITE_CONSTRAINT_NOTNULL = (19 | (5 << 8));

        [NativeTypeName("#define SQLITE_CONSTRAINT_PRIMARYKEY (SQLITE_CONSTRAINT | (6<<8))")]
        public const int SQLITE_CONSTRAINT_PRIMARYKEY = (19 | (6 << 8));

        [NativeTypeName("#define SQLITE_CONSTRAINT_TRIGGER (SQLITE_CONSTRAINT | (7<<8))")]
        public const int SQLITE_CONSTRAINT_TRIGGER = (19 | (7 << 8));

        [NativeTypeName("#define SQLITE_CONSTRAINT_UNIQUE (SQLITE_CONSTRAINT | (8<<8))")]
        public const int SQLITE_CONSTRAINT_UNIQUE = (19 | (8 << 8));

        [NativeTypeName("#define SQLITE_CONSTRAINT_VTAB (SQLITE_CONSTRAINT | (9<<8))")]
        public const int SQLITE_CONSTRAINT_VTAB = (19 | (9 << 8));

        [NativeTypeName("#define SQLITE_CONSTRAINT_ROWID (SQLITE_CONSTRAINT |(10<<8))")]
        public const int SQLITE_CONSTRAINT_ROWID = (19 | (10 << 8));

        [NativeTypeName("#define SQLITE_CONSTRAINT_PINNED (SQLITE_CONSTRAINT |(11<<8))")]
        public const int SQLITE_CONSTRAINT_PINNED = (19 | (11 << 8));

        [NativeTypeName("#define SQLITE_CONSTRAINT_DATATYPE (SQLITE_CONSTRAINT |(12<<8))")]
        public const int SQLITE_CONSTRAINT_DATATYPE = (19 | (12 << 8));

        [NativeTypeName("#define SQLITE_NOTICE_RECOVER_WAL (SQLITE_NOTICE | (1<<8))")]
        public const int SQLITE_NOTICE_RECOVER_WAL = (27 | (1 << 8));

        [NativeTypeName("#define SQLITE_NOTICE_RECOVER_ROLLBACK (SQLITE_NOTICE | (2<<8))")]
        public const int SQLITE_NOTICE_RECOVER_ROLLBACK = (27 | (2 << 8));

        [NativeTypeName("#define SQLITE_NOTICE_RBU (SQLITE_NOTICE | (3<<8))")]
        public const int SQLITE_NOTICE_RBU = (27 | (3 << 8));

        [NativeTypeName("#define SQLITE_WARNING_AUTOINDEX (SQLITE_WARNING | (1<<8))")]
        public const int SQLITE_WARNING_AUTOINDEX = (28 | (1 << 8));

        [NativeTypeName("#define SQLITE_AUTH_USER (SQLITE_AUTH | (1<<8))")]
        public const int SQLITE_AUTH_USER = (23 | (1 << 8));

        [NativeTypeName("#define SQLITE_OK_LOAD_PERMANENTLY (SQLITE_OK | (1<<8))")]
        public const int SQLITE_OK_LOAD_PERMANENTLY = (0 | (1 << 8));

        [NativeTypeName("#define SQLITE_OK_SYMLINK (SQLITE_OK | (2<<8))")]
        public const int SQLITE_OK_SYMLINK = (0 | (2 << 8));

        [NativeTypeName("#define SQLITE_OPEN_READONLY 0x00000001")]
        public const int SQLITE_OPEN_READONLY = 0x00000001;

        [NativeTypeName("#define SQLITE_OPEN_READWRITE 0x00000002")]
        public const int SQLITE_OPEN_READWRITE = 0x00000002;

        [NativeTypeName("#define SQLITE_OPEN_CREATE 0x00000004")]
        public const int SQLITE_OPEN_CREATE = 0x00000004;

        [NativeTypeName("#define SQLITE_OPEN_DELETEONCLOSE 0x00000008")]
        public const int SQLITE_OPEN_DELETEONCLOSE = 0x00000008;

        [NativeTypeName("#define SQLITE_OPEN_EXCLUSIVE 0x00000010")]
        public const int SQLITE_OPEN_EXCLUSIVE = 0x00000010;

        [NativeTypeName("#define SQLITE_OPEN_AUTOPROXY 0x00000020")]
        public const int SQLITE_OPEN_AUTOPROXY = 0x00000020;

        [NativeTypeName("#define SQLITE_OPEN_URI 0x00000040")]
        public const int SQLITE_OPEN_URI = 0x00000040;

        [NativeTypeName("#define SQLITE_OPEN_MEMORY 0x00000080")]
        public const int SQLITE_OPEN_MEMORY = 0x00000080;

        [NativeTypeName("#define SQLITE_OPEN_MAIN_DB 0x00000100")]
        public const int SQLITE_OPEN_MAIN_DB = 0x00000100;

        [NativeTypeName("#define SQLITE_OPEN_TEMP_DB 0x00000200")]
        public const int SQLITE_OPEN_TEMP_DB = 0x00000200;

        [NativeTypeName("#define SQLITE_OPEN_TRANSIENT_DB 0x00000400")]
        public const int SQLITE_OPEN_TRANSIENT_DB = 0x00000400;

        [NativeTypeName("#define SQLITE_OPEN_MAIN_JOURNAL 0x00000800")]
        public const int SQLITE_OPEN_MAIN_JOURNAL = 0x00000800;

        [NativeTypeName("#define SQLITE_OPEN_TEMP_JOURNAL 0x00001000")]
        public const int SQLITE_OPEN_TEMP_JOURNAL = 0x00001000;

        [NativeTypeName("#define SQLITE_OPEN_SUBJOURNAL 0x00002000")]
        public const int SQLITE_OPEN_SUBJOURNAL = 0x00002000;

        [NativeTypeName("#define SQLITE_OPEN_SUPER_JOURNAL 0x00004000")]
        public const int SQLITE_OPEN_SUPER_JOURNAL = 0x00004000;

        [NativeTypeName("#define SQLITE_OPEN_NOMUTEX 0x00008000")]
        public const int SQLITE_OPEN_NOMUTEX = 0x00008000;

        [NativeTypeName("#define SQLITE_OPEN_FULLMUTEX 0x00010000")]
        public const int SQLITE_OPEN_FULLMUTEX = 0x00010000;

        [NativeTypeName("#define SQLITE_OPEN_SHAREDCACHE 0x00020000")]
        public const int SQLITE_OPEN_SHAREDCACHE = 0x00020000;

        [NativeTypeName("#define SQLITE_OPEN_PRIVATECACHE 0x00040000")]
        public const int SQLITE_OPEN_PRIVATECACHE = 0x00040000;

        [NativeTypeName("#define SQLITE_OPEN_WAL 0x00080000")]
        public const int SQLITE_OPEN_WAL = 0x00080000;

        [NativeTypeName("#define SQLITE_OPEN_NOFOLLOW 0x01000000")]
        public const int SQLITE_OPEN_NOFOLLOW = 0x01000000;

        [NativeTypeName("#define SQLITE_OPEN_EXRESCODE 0x02000000")]
        public const int SQLITE_OPEN_EXRESCODE = 0x02000000;

        [NativeTypeName("#define SQLITE_OPEN_MASTER_JOURNAL 0x00004000")]
        public const int SQLITE_OPEN_MASTER_JOURNAL = 0x00004000;

        [NativeTypeName("#define SQLITE_LIMIT_LENGTH 0")]
        public const int SQLITE_LIMIT_LENGTH = 0;

        [NativeTypeName("#define SQLITE_LIMIT_SQL_LENGTH 1")]
        public const int SQLITE_LIMIT_SQL_LENGTH = 1;

        [NativeTypeName("#define SQLITE_LIMIT_COLUMN 2")]
        public const int SQLITE_LIMIT_COLUMN = 2;

        [NativeTypeName("#define SQLITE_LIMIT_EXPR_DEPTH 3")]
        public const int SQLITE_LIMIT_EXPR_DEPTH = 3;

        [NativeTypeName("#define SQLITE_LIMIT_COMPOUND_SELECT 4")]
        public const int SQLITE_LIMIT_COMPOUND_SELECT = 4;

        [NativeTypeName("#define SQLITE_LIMIT_VDBE_OP 5")]
        public const int SQLITE_LIMIT_VDBE_OP = 5;

        [NativeTypeName("#define SQLITE_LIMIT_FUNCTION_ARG 6")]
        public const int SQLITE_LIMIT_FUNCTION_ARG = 6;

        [NativeTypeName("#define SQLITE_LIMIT_ATTACHED 7")]
        public const int SQLITE_LIMIT_ATTACHED = 7;

        [NativeTypeName("#define SQLITE_LIMIT_LIKE_PATTERN_LENGTH 8")]
        public const int SQLITE_LIMIT_LIKE_PATTERN_LENGTH = 8;

        [NativeTypeName("#define SQLITE_LIMIT_VARIABLE_NUMBER 9")]
        public const int SQLITE_LIMIT_VARIABLE_NUMBER = 9;

        [NativeTypeName("#define SQLITE_LIMIT_TRIGGER_DEPTH 10")]
        public const int SQLITE_LIMIT_TRIGGER_DEPTH = 10;

        [NativeTypeName("#define SQLITE_LIMIT_WORKER_THREADS 11")]
        public const int SQLITE_LIMIT_WORKER_THREADS = 11;

        [NativeTypeName("#define SQLITE_PREPARE_PERSISTENT 0x01")]
        public const int SQLITE_PREPARE_PERSISTENT = 0x01;

        [NativeTypeName("#define SQLITE_PREPARE_NORMALIZE 0x02")]
        public const int SQLITE_PREPARE_NORMALIZE = 0x02;

        [NativeTypeName("#define SQLITE_PREPARE_NO_VTAB 0x04")]
        public const int SQLITE_PREPARE_NO_VTAB = 0x04;

        [NativeTypeName("#define SQLITE_PREPARE_DONT_LOG 0x10")]
        public const int SQLITE_PREPARE_DONT_LOG = 0x10;

        [NativeTypeName("#define SQLITE_INTEGER 1")]
        public const int SQLITE_INTEGER = 1;

        [NativeTypeName("#define SQLITE_FLOAT 2")]
        public const int SQLITE_FLOAT = 2;

        [NativeTypeName("#define SQLITE_BLOB 4")]
        public const int SQLITE_BLOB = 4;

        [NativeTypeName("#define SQLITE_NULL 5")]
        public const int SQLITE_NULL = 5;

        [NativeTypeName("#define SQLITE_TEXT 3")]
        public const int SQLITE_TEXT = 3;

        [NativeTypeName("#define SQLITE_TXN_NONE 0")]
        public const int SQLITE_TXN_NONE = 0;

        [NativeTypeName("#define SQLITE_TXN_READ 1")]
        public const int SQLITE_TXN_READ = 1;

        [NativeTypeName("#define SQLITE_TXN_WRITE 2")]
        public const int SQLITE_TXN_WRITE = 2;
    }
}
