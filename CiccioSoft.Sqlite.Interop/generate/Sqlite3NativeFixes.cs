namespace CiccioSoft.Sqlite.Interop.Native
{
    public static unsafe partial class Sqlite3Native
    {
        // // Definisci qui la stringa reale in modo pulito e moderno
        internal const string SQLITE_DLL = "e_sqlite3";
        // internal const string SQLITE_DLL = "libsqlite3-0";
        // internal const string SQLITE_DLL = "winsqlite3";
        // internal const string SQLITE_DLL = "sqlite3";

        [NativeTypeName("#define SQLITE_STATIC ((sqlite3_destructor_type)0)")]
        public static readonly delegate* unmanaged[Cdecl]<void*, void> SQLITE_STATIC = ((delegate* unmanaged[Cdecl]<void*, void>)(0));

        [NativeTypeName("#define SQLITE_TRANSIENT ((sqlite3_destructor_type)-1)")]
        public static readonly delegate* unmanaged[Cdecl]<void*, void> SQLITE_TRANSIENT = ((delegate* unmanaged[Cdecl]<void*, void>)(-1));
    }
}
