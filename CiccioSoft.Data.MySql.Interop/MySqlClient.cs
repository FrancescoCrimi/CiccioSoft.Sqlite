// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Runtime.InteropServices;
using System.Text;
using CiccioSoft.Data.MySql.Interop.Native;

namespace CiccioSoft.Data.MySql.Interop
{
    /// <summary>
    /// Thin wrapper around a native <c>MYSQL*</c> handle.
    /// </summary>
    public sealed class MySqlClient : IDisposable
    {
        private IntPtr _handle;

        private MySqlClient(IntPtr handle)
        {
            _handle = handle;
        }

        public static MySqlClient Open(string host, uint port, string user, string password, string database)
        {
            IntPtr handle = NativeMySqlClient.mysql_init(IntPtr.Zero);
            if (handle == IntPtr.Zero)
            {
                throw new MySqlInteropException("Unable to allocate MYSQL handle via mysql_init.");
            }

            byte[] hostBytes = BuildUtf8NullTerminated(host);
            byte[] userBytes = BuildUtf8NullTerminated(user);
            byte[] passwordBytes = BuildUtf8NullTerminated(password);
            byte[] databaseBytes = BuildUtf8NullTerminated(database);

            IntPtr connected;
            unsafe
            {
                fixed (byte* phost = hostBytes)
                fixed (byte* puser = userBytes)
                fixed (byte* ppassword = passwordBytes)
                fixed (byte* pdatabase = databaseBytes)
                {
                    connected = NativeMySqlClient.mysql_real_connect(
                        handle,
                        phost,
                        puser,
                        ppassword,
                        pdatabase,
                        port,
                        unix_socket: (byte*)IntPtr.Zero,
                        client_flag: 0);
                }
            }

            if (connected == IntPtr.Zero)
            {
                string error = GetLastError(handle);
                NativeMySqlClient.mysql_close(handle);
                throw new MySqlInteropException($"mysql_real_connect failed: {error}");
            }

            return new MySqlClient(handle);
        }

        public void Ping()
        {
            EnsureNotDisposed();
            int result = NativeMySqlClient.mysql_ping(_handle);
            if (result != 0)
            {
                throw new MySqlInteropException($"mysql_ping failed: {GetLastError(_handle)}");
            }
        }

        public void Dispose()
        {
            if (_handle == IntPtr.Zero)
            {
                return;
            }

            NativeMySqlClient.mysql_close(_handle);
            _handle = IntPtr.Zero;
            GC.SuppressFinalize(this);
        }

        private void EnsureNotDisposed()
        {
            if (_handle == IntPtr.Zero)
            {
                throw new ObjectDisposedException(nameof(MySqlClient));
            }
        }

        private static string GetLastError(IntPtr handle)
        {
            IntPtr ptr = NativeMySqlClient.mysql_error(handle);
            return ptr == IntPtr.Zero
                ? "unknown error"
                : Marshal.PtrToStringUTF8(ptr) ?? "unknown error";
        }

        private static byte[] BuildUtf8NullTerminated(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            byte[] nullTerminated = new byte[bytes.Length + 1];
            bytes.CopyTo(nullTerminated, 0);
            return nullTerminated;
        }
    }
}
