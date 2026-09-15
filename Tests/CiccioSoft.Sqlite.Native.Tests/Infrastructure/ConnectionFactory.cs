// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

namespace CiccioSoft.Sqlite.Native.Tests.Infrastructure;

/// <summary>
/// Factory helpers for enterprise-grade, isolated in-memory connections.
/// </summary>
internal static class ConnectionFactory
{
    /// <summary>
    /// Opens a private in-memory database (not shared across connections).
    /// </summary>
    public static Native.Connection OpenMemory()
        => Native.Connection.Open(":memory:", OpenFlags.ReadWrite | OpenFlags.Create);

    /// <summary>
    /// Opens a named shared in-memory database via URI filename.
    /// Multiple connections with the same name share the same page cache.
    /// </summary>
    public static Native.Connection OpenSharedMemory(string name)
        => Native.Connection.Open(
            $"file:{name}?mode=memory&cache=shared",
            OpenFlags.ReadWrite | OpenFlags.Create);

    public static Native.Connection OpenWithSchema(string ddl)
    {
        var connection = OpenMemory();
        connection.Execute(ddl);
        return connection;
    }
}
