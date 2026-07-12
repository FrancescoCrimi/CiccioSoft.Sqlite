// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace CiccioSoft.Sqlite.Interop.Native;

public sealed unsafe class VTableCache
{
    // ── Singleton ─────────────────────────────────────────────────────────
    private static readonly Lazy<VTableCache> _lazy =
        new(static () => new VTableCache(),
            LazyThreadSafetyMode.ExecutionAndPublication);

    public static VTableCache Instance => _lazy.Value;

    // ── Puntatori alle VTable (accesso diretto, hot path) ─────────────────
    public sqlite_vtable Db { get; }
    public sqlite_stmt_vtable Stmt { get; }
    public sqlite_backup_vtable Backup { get; }

    // ── Costruttore privato ───────────────────────────────────────────────
    private VTableCache()
    {
        // string libFile  = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)  ? "libsqlite3glue.dll"
        string libFile = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "sqlite3glue.dll"
                        : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "sqlite3glue.dylib"
                        : "sqlite3glue.so";
        nint libHandle = NativeLibrary.Load(libFile);

        // Bootstrap: una sola chiamata P/Invoke per nome
        var getVt = (delegate* unmanaged[Cdecl]<out vtable, void>)
            NativeLibrary.GetExport(libHandle, "get_vtable");

        vtable table;
        getVt(out table);

        Db = table.sqlite;
        Stmt = table.stmt;
        Backup = table.backup;
    }
}
