// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace CiccioSoft.Sqlite.Interop.Com;

public sealed unsafe class VTableCache
{
    // ── Singleton ─────────────────────────────────────────────────────────
    private static readonly Lazy<VTableCache> _lazy =
        new(static () => new VTableCache(),
            LazyThreadSafetyMode.ExecutionAndPublication);

    public static VTableCache Instance => _lazy.Value;

    // ── Puntatori alle VTable (accesso diretto, hot path) ─────────────────
    public SqliteVTable Db { get; }
    public StmtVTable Stmt { get; }
    public BackupVTable Backup { get; }

    // ── Costruttore privato ───────────────────────────────────────────────
    private VTableCache()
    {
        // string libFile  = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)  ? "libSQLiteGlue.dll"
        string libFile = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "libSQLiteGlue.dll"
                        : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "libSQLiteGlue.dylib"
                        : "libSQLiteGlue.so";
        nint libHandle = NativeLibrary.Load(libFile);

        // Bootstrap: una sola chiamata P/Invoke per nome
        var getVt = (delegate* unmanaged[Cdecl]<out VTable, void>)
            NativeLibrary.GetExport(libHandle, "get_vtable");

        VTable table;
        getVt(out table);

        Db = table.Db;
        Stmt = table.Stmt;
        Backup = table.Backup;
    }
}
