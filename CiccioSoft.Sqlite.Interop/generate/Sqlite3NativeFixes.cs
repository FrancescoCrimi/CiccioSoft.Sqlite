using System;
using System.Runtime.InteropServices;

namespace CiccioSoft.Sqlite.Interop.Native
{
    internal static unsafe partial class Sqlite3Native
    {
        // // Definisci qui la stringa reale in modo pulito e moderno
        // public const string SQLITE_DLL = "e_sqlite3";
        // public const string SQLITE_DLL = "libsqlite3-0";

        // [NativeTypeName("#define SQLITE_STATIC ((sqlite3_destructor_type)0)")]
        // public static readonly nint SQLITE_STATIC = ((nint)(0));

        // [NativeTypeName("#define SQLITE_TRANSIENT ((sqlite3_destructor_type)-1)")]
        // public static readonly nint SQLITE_TRANSIENT = ((nint)(-1));

        [NativeTypeName("#define SQLITE_STATIC ((sqlite3_destructor_type)0)")]
        public static readonly delegate* unmanaged[Cdecl]<void*, void> SQLITE_STATIC = ((delegate* unmanaged[Cdecl]<void*, void>)(0));

        [NativeTypeName("#define SQLITE_TRANSIENT ((sqlite3_destructor_type)-1)")]
        public static readonly delegate* unmanaged[Cdecl]<void*, void> SQLITE_TRANSIENT = ((delegate* unmanaged[Cdecl]<void*, void>)(-1));
    }
}