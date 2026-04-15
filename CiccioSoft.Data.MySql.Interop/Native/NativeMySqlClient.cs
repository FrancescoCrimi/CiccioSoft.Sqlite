// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Runtime.InteropServices;

namespace CiccioSoft.Data.MySql.Interop.Native
{
    internal static unsafe class NativeMySqlClient
    {
        [DllImport(MySqlClientLibrary.Name, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr mysql_init(IntPtr mysql);

        [DllImport(MySqlClientLibrary.Name, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr mysql_real_connect(
            IntPtr mysql,
            byte* host,
            byte* user,
            byte* passwd,
            byte* db,
            uint port,
            byte* unix_socket,
            ulong client_flag);

        [DllImport(MySqlClientLibrary.Name, CallingConvention = CallingConvention.Cdecl)]
        public static extern int mysql_ping(IntPtr mysql);

        [DllImport(MySqlClientLibrary.Name, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr mysql_error(IntPtr mysql);

        [DllImport(MySqlClientLibrary.Name, CallingConvention = CallingConvention.Cdecl)]
        public static extern void mysql_close(IntPtr mysql);
    }
}
