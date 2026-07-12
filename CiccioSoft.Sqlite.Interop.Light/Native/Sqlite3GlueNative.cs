using System.Runtime.InteropServices;

namespace CiccioSoft.Sqlite.Interop.Native
{
    public static unsafe partial class Sqlite3GlueNative
    {
        [DllImport("sqlite3glue", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void get_vtable(vtable* table);
    }
}
