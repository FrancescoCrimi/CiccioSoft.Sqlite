namespace CiccioSoft.Sqlite.Interop.Native
{
    public unsafe partial struct sqlite_vtable
    {
        [NativeTypeName("const char *(*)(void)")]
        public delegate* unmanaged[Cdecl]<byte*> libversion;

        [NativeTypeName("int (*)(void)")]
        public delegate* unmanaged[Cdecl]<int> libversion_number;

        [NativeTypeName("int (*)(sqlite3 *)")]
        public delegate* unmanaged[Cdecl]<sqlite3*, int> close;

        [NativeTypeName("int (*)(sqlite3 *, const char *, int (*)(void *, int, char **, char **), void *, char **)")]
        public delegate* unmanaged[Cdecl]<sqlite3*, byte*, delegate* unmanaged[Cdecl]<void*, int, byte**, byte**, int>, void*, byte**, int> exec;

        [NativeTypeName("int (*)(sqlite3 *, int)")]
        public delegate* unmanaged[Cdecl]<sqlite3*, int, int> extended_result_codes;

        [NativeTypeName("sqlite3_int64 (*)(sqlite3 *)")]
        public delegate* unmanaged[Cdecl]<sqlite3*, long> last_insert_rowid;

        [NativeTypeName("int (*)(sqlite3 *)")]
        public delegate* unmanaged[Cdecl]<sqlite3*, int> changes;

        [NativeTypeName("sqlite3_int64 (*)(sqlite3 *)")]
        public delegate* unmanaged[Cdecl]<sqlite3*, long> changes64;

        [NativeTypeName("int (*)(sqlite3 *)")]
        public delegate* unmanaged[Cdecl]<sqlite3*, int> total_changes;

        [NativeTypeName("sqlite3_int64 (*)(sqlite3 *)")]
        public delegate* unmanaged[Cdecl]<sqlite3*, long> total_changes64;

        [NativeTypeName("void (*)(sqlite3 *)")]
        public delegate* unmanaged[Cdecl]<sqlite3*, void> interrupt;

        [NativeTypeName("int (*)(sqlite3 *)")]
        public delegate* unmanaged[Cdecl]<sqlite3*, int> is_interrupted;

        [NativeTypeName("int (*)(sqlite3 *, int)")]
        public delegate* unmanaged[Cdecl]<sqlite3*, int, int> busy_timeout;

        [NativeTypeName("void (*)(void *)")]
        public delegate* unmanaged[Cdecl]<void*, void> free;

        [NativeTypeName("int (*)(const char *, sqlite3 **, int, const char *)")]
        public delegate* unmanaged[Cdecl]<byte*, sqlite3**, int, byte*, int> open;

        [NativeTypeName("int (*)(sqlite3 *)")]
        public delegate* unmanaged[Cdecl]<sqlite3*, int> errcode;

        [NativeTypeName("int (*)(sqlite3 *)")]
        public delegate* unmanaged[Cdecl]<sqlite3*, int> extended_errcode;

        [NativeTypeName("const char *(*)(sqlite3 *)")]
        public delegate* unmanaged[Cdecl]<sqlite3*, byte*> errmsg;

        [NativeTypeName("const char *(*)(int)")]
        public delegate* unmanaged[Cdecl]<int, byte*> errstr;

        [NativeTypeName("int (*)(sqlite3 *)")]
        public delegate* unmanaged[Cdecl]<sqlite3*, int> error_offset;

        [NativeTypeName("int (*)(sqlite3 *, int, int)")]
        public delegate* unmanaged[Cdecl]<sqlite3*, int, int, int> limit;

        [NativeTypeName("int (*)(sqlite3 *, const char *, int, unsigned int, sqlite3_stmt **, const char **)")]
        public delegate* unmanaged[Cdecl]<sqlite3*, byte*, int, uint, sqlite3_stmt**, byte**, int> prepare;

        [NativeTypeName("int (*)(sqlite3 *)")]
        public delegate* unmanaged[Cdecl]<sqlite3*, int> get_autocommit;

        [NativeTypeName("int (*)(sqlite3 *, const char *)")]
        public delegate* unmanaged[Cdecl]<sqlite3*, byte*, int> db_readonly;

        [NativeTypeName("int (*)(sqlite3 *, const char *)")]
        public delegate* unmanaged[Cdecl]<sqlite3*, byte*, int> txn_state;

        [NativeTypeName("int (*)(sqlite3 *, const char *, const char *, const char *, const char **, const char **, int *, int *, int *)")]
        public delegate* unmanaged[Cdecl]<sqlite3*, byte*, byte*, byte*, byte**, byte**, int*, int*, int*, int> table_column_metadata;
    }
}
