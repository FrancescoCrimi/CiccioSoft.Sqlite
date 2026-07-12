namespace CiccioSoft.Sqlite.Interop.Native
{
    public partial struct vtable
    {
        public int version;

        [NativeTypeName("struct sqlite_vtable")]
        public sqlite_vtable sqlite;

        [NativeTypeName("struct sqlite_stmt_vtable")]
        public sqlite_stmt_vtable stmt;

        [NativeTypeName("struct sqlite_backup_vtable")]
        public sqlite_backup_vtable backup;
    }
}
