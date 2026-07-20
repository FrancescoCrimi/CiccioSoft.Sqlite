// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Runtime.InteropServices;

namespace CiccioSoft.Interop.Sqlite;

public sealed unsafe class SafeBackupHandle : SafeHandle
{
    internal SafeBackupHandle(sqlite3_backup* sqlite3_backup)
        : base((nint)sqlite3_backup, true)
    {
    }

    public override bool IsInvalid => handle == nint.Zero;

    internal sqlite3_backup* AsStructPointer() => (sqlite3_backup*)handle;

    protected override bool ReleaseHandle()
    {
        return NativeMethods.sqlite3_backup_finish((sqlite3_backup*)handle) == NativeMethods.SQLITE_OK;
    }
}

public sealed unsafe class Backup : IDisposable
{
    private readonly SafeBackupHandle _handle;

    internal Backup(SafeBackupHandle handle)
    {
        _handle = handle;
    }

    public static Backup InitBackup(Connection destination, Connection source, string destinationDatabaseName = "main", string sourceDatabaseName = "main")
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (destination.Handle.IsInvalid) throw new ObjectDisposedException(nameof(Connection));

        ArgumentNullException.ThrowIfNull(source);
        if (source.Handle.IsInvalid) throw new ObjectDisposedException(nameof(Connection));

        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDatabaseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDatabaseName);

        using var destinationNameBuffer = new Utf8SafeStackBuffer(destinationDatabaseName, stackalloc byte[512]);
        using var sourceNameBuffer = new Utf8SafeStackBuffer(sourceDatabaseName, stackalloc byte[512]);

        fixed (byte* pDest = destinationNameBuffer, pSource = sourceNameBuffer)
        {
            sqlite3_backup* backupHandle = NativeMethods.sqlite3_backup_init(
                destination.Handle.AsStructPointer(),
                pDest,
                source.Handle.AsStructPointer(),
                pSource);

            if ((nint)backupHandle == nint.Zero)
            {
                throw new EngineException(
                    (ExtendedResult)NativeMethods.sqlite3_errcode(destination.Handle.AsStructPointer()),
                    destination.Handle,
                    "SQLite backup init");
            }

            return new Backup(new SafeBackupHandle(backupHandle));
        }
    }

    public ExtendedResult Step(int pages = -1)
    {
        ThrowIfInvalid();
        return (ExtendedResult)NativeMethods.sqlite3_backup_step(_handle.AsStructPointer(), pages);
    }

    public int Remaining()
    {
        ThrowIfInvalid();
        return NativeMethods.sqlite3_backup_remaining(_handle.AsStructPointer());
    }

    public int PageCount()
    {
        ThrowIfInvalid();
        return NativeMethods.sqlite3_backup_pagecount(_handle.AsStructPointer());
    }

    public void Finish()
    {
        // if (_handle.IsClosed || _handle.IsInvalid)
        // {
        //     return SqliteExtendedResult.OK;
        // }

        // SqliteResult result = (SqliteResult)NativeMethods.sqlite3_backup_finish(_handle.AsStructPointer());
        // _handle.SetHandleAsInvalid();
        // _handle.Dispose();
        // return result;
        Dispose();
    }

    private void ThrowIfInvalid()
    {
        if (_handle.IsClosed || _handle.IsInvalid)
            throw new ObjectDisposedException(nameof(Backup));
    }

    public void Dispose() => _handle.Dispose();
}
