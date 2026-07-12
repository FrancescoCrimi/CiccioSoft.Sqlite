namespace CiccioSoft.Sqlite.Interop.Native
{
    public unsafe partial struct sqlite_backup_vtable
    {
        [NativeTypeName("sqlite3_backup *(*)(sqlite3 *, const char *, sqlite3 *, const char *)")]
        public delegate* unmanaged[Cdecl]<sqlite3*, byte*, sqlite3*, byte*, sqlite3_backup*> backup_init;

        [NativeTypeName("int (*)(sqlite3_backup *, int)")]
        public delegate* unmanaged[Cdecl]<sqlite3_backup*, int, int> backup_step;

        [NativeTypeName("int (*)(sqlite3_backup *)")]
        public delegate* unmanaged[Cdecl]<sqlite3_backup*, int> backup_finish;

        [NativeTypeName("int (*)(sqlite3_backup *)")]
        public delegate* unmanaged[Cdecl]<sqlite3_backup*, int> backup_remaining;

        [NativeTypeName("int (*)(sqlite3_backup *)")]
        public delegate* unmanaged[Cdecl]<sqlite3_backup*, int> backup_pagecount;
    }
}
