// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.IO;
using CiccioSoft.Sqlite.Native;

namespace CiccioSoft.Sqlite.Tests;

/// <summary>
/// Owns a unique temporary SQLite database file and deletes it on dispose.
/// </summary>
internal sealed class TestDatabase : IDisposable
{
    public string Path { get; }

    public TestDatabase(string? prefix = null)
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"{prefix ?? "interop"}-{Guid.NewGuid():N}.db");
    }

    public Connection Open(OpenFlags flags = OpenFlags.ReadWrite | OpenFlags.Create)
        => Connection.Open(Path, flags);

    /// <summary>
    /// Opens a private in-memory database (not shared across connections).
    /// </summary>
    public static Connection OpenMemory()
        => Connection.Open(":memory:", OpenFlags.ReadWrite | OpenFlags.Create);

    /// <summary>
    /// Opens a named shared in-memory database via URI filename.
    /// Multiple connections with the same name share the same page cache.
    /// </summary>
    public static Connection OpenSharedMemory(string name)
        => Connection.Open(
            $"file:{name}?mode=memory&cache=shared",
            OpenFlags.ReadWrite | OpenFlags.Create);

    public void Dispose()
    {
        try { File.Delete(Path); } catch { }
        try { File.Delete(Path + "-wal"); } catch { }
        try { File.Delete(Path + "-shm"); } catch { }
        try { File.Delete(Path + "-journal"); } catch { }
    }
}
