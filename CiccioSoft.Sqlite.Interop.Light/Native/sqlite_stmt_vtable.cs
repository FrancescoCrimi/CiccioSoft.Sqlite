namespace CiccioSoft.Sqlite.Interop.Native
{
    public unsafe partial struct sqlite_stmt_vtable
    {
        [NativeTypeName("const char *(*)(sqlite3_stmt *)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, byte*> sql;

        [NativeTypeName("char *(*)(sqlite3_stmt *)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, byte*> expanded_sql;

        [NativeTypeName("int (*)(sqlite3_stmt *)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int> stmt_readonly;

        [NativeTypeName("int (*)(sqlite3_stmt *)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int> stmt_busy;

        [NativeTypeName("int (*)(sqlite3_stmt *, int, const void *, int, void (*)(void *))")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, void*, int, delegate* unmanaged[Cdecl]<void*, void>, int> bind_blob;

        [NativeTypeName("int (*)(sqlite3_stmt *, int, double)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, double, int> bind_double;

        [NativeTypeName("int (*)(sqlite3_stmt *, int, int)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, int, int> bind_int;

        [NativeTypeName("int (*)(sqlite3_stmt *, int, sqlite3_int64)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, long, int> bind_int64;

        [NativeTypeName("int (*)(sqlite3_stmt *, int)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, int> bind_null;

        [NativeTypeName("int (*)(sqlite3_stmt *, int, const char *, int, void (*)(void *))")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, byte*, int, delegate* unmanaged[Cdecl]<void*, void>, int> bind_text;

        [NativeTypeName("int (*)(sqlite3_stmt *, int, const sqlite3_value *)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, sqlite3_value*, int> bind_value;

        [NativeTypeName("int (*)(sqlite3_stmt *)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int> bind_parameter_count;

        [NativeTypeName("const char *(*)(sqlite3_stmt *, int)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, byte*> bind_parameter_name;

        [NativeTypeName("int (*)(sqlite3_stmt *, const char *)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, byte*, int> bind_parameter_index;

        [NativeTypeName("int (*)(sqlite3_stmt *)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int> clear_bindings;

        [NativeTypeName("int (*)(sqlite3_stmt *)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int> column_count;

        [NativeTypeName("const char *(*)(sqlite3_stmt *, int)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, byte*> column_name;

        [NativeTypeName("const char *(*)(sqlite3_stmt *, int)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, byte*> column_database_name;

        [NativeTypeName("const char *(*)(sqlite3_stmt *, int)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, byte*> column_table_name;

        [NativeTypeName("const char *(*)(sqlite3_stmt *, int)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, byte*> column_origin_name;

        [NativeTypeName("const char *(*)(sqlite3_stmt *, int)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, byte*> column_decltype;

        [NativeTypeName("int (*)(sqlite3_stmt *)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int> step;

        [NativeTypeName("const void *(*)(sqlite3_stmt *, int)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, void*> column_blob;

        [NativeTypeName("double (*)(sqlite3_stmt *, int)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, double> column_double;

        [NativeTypeName("int (*)(sqlite3_stmt *, int)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, int> column_int;

        [NativeTypeName("sqlite3_int64 (*)(sqlite3_stmt *, int)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, long> column_int64;

        [NativeTypeName("const unsigned char *(*)(sqlite3_stmt *, int)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, byte*> column_text;

        [NativeTypeName("sqlite3_value *(*)(sqlite3_stmt *, int)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, sqlite3_value*> column_value;

        [NativeTypeName("int (*)(sqlite3_stmt *, int)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, int> column_bytes;

        [NativeTypeName("int (*)(sqlite3_stmt *, int)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int, int> column_type;

        [NativeTypeName("int (*)(sqlite3_stmt *)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int> finalize;

        [NativeTypeName("int (*)(sqlite3_stmt *)")]
        public delegate* unmanaged[Cdecl]<sqlite3_stmt*, int> reset;
    }
}
