// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Runtime.InteropServices;
using CiccioSoft.Sqlite.Interop.Native;

namespace CiccioSoft.Sqlite.Interop.Light;

public sealed unsafe class Sqlite3BackupSafeHandle : SafeHandle
{
    internal Sqlite3BackupSafeHandle(sqlite3_backup* sqlite3_backup)
        : base((nint)sqlite3_backup, true)
    {
    }

    public override bool IsInvalid => handle == nint.Zero;

    public sqlite3_backup* AsStructPointer() => (sqlite3_backup*)handle;

    protected override bool ReleaseHandle()
    {
        return VTableCache.Instance.Backup.backup_finish((sqlite3_backup*)handle) == Sqlite3Native.SQLITE_OK;
    }
}

public sealed unsafe class Sqlite3Backup : IDisposable
{
    private readonly Sqlite3BackupSafeHandle _handle;

    internal Sqlite3Backup(Sqlite3BackupSafeHandle handle)
    {
        _handle = handle;
    }

    public static Sqlite3Backup InitBackup(Sqlite3 destination, Sqlite3 source, string destinationDatabaseName = "main", string sourceDatabaseName = "main")
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (destination.Handle.IsInvalid) throw new ObjectDisposedException(nameof(Sqlite3));

        ArgumentNullException.ThrowIfNull(source);
        if (source.Handle.IsInvalid) throw new ObjectDisposedException(nameof(Sqlite3));

        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDatabaseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDatabaseName);

        using var destinationNameBuffer = new Utf8SafeStackBuffer(destinationDatabaseName, stackalloc byte[512]);
        using var sourceNameBuffer = new Utf8SafeStackBuffer(sourceDatabaseName, stackalloc byte[512]);

        fixed (byte* pDest = destinationNameBuffer, pSource = sourceNameBuffer)
        {
            sqlite3_backup* backupHandle = VTableCache.Instance.Backup.backup_init(
                destination.Handle.AsStructPointer(),
                pDest,
                source.Handle.AsStructPointer(),
                pSource);

            if ((nint)backupHandle == nint.Zero)
            {
                throw new SqliteInteropException(
                    (SqliteResult)VTableCache.Instance.Db.errcode(destination.Handle.AsStructPointer()),
                    destination.Handle,
                    "SQLite backup init");
            }

            return new Sqlite3Backup(new Sqlite3BackupSafeHandle(backupHandle));
        }
    }

    public SqliteResult Step(int pages = -1)
    {
        ThrowIfInvalid();
        return (SqliteResult)VTableCache.Instance.Backup.backup_step(_handle.AsStructPointer(), pages);
    }

    public int Remaining()
    {
        ThrowIfInvalid();
        return VTableCache.Instance.Backup.backup_remaining(_handle.AsStructPointer());
    }

    public int PageCount()
    {
        ThrowIfInvalid();
        return VTableCache.Instance.Backup.backup_pagecount(_handle.AsStructPointer());
    }

    public SqliteResult Finish()
    {
        if (_handle.IsClosed || _handle.IsInvalid)
        {
            return SqliteResult.OK;
        }

        SqliteResult result = (SqliteResult)VTableCache.Instance.Backup.backup_finish(_handle.AsStructPointer());
        _handle.SetHandleAsInvalid();
        _handle.Dispose();
        return result;
    }

    private void ThrowIfInvalid()
    {
        if (_handle.IsClosed || _handle.IsInvalid)
            throw new ObjectDisposedException(nameof(Sqlite3Backup));
    }

    public void Dispose() => _handle.Dispose();
}
