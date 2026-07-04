// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using Microsoft.Win32.SafeHandles;
using CiccioSoft.Sqlite.Interop.Native;

namespace CiccioSoft.Sqlite.Interop;

public sealed class Sqlite3BackupHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    internal Sqlite3BackupHandle(nint handle) : base(true)
    {
        SetHandle(handle);
    }
    protected override bool ReleaseHandle()
    {
        Sqlite3Native.sqlite3_backup_finish(handle);
        return true;
    }
}

public sealed unsafe class Sqlite3Backup : IDisposable
{
    private readonly Sqlite3BackupHandle _handle;

    internal Sqlite3Backup(Sqlite3BackupHandle handle)
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
            nint destinationHandle = destination.Handle.DangerousGetHandle();
            nint sourceHandle = source.Handle.DangerousGetHandle();

            nint backupHandle = Sqlite3Native.sqlite3_backup_init(
                destinationHandle,
                pDest,
                sourceHandle,
                pSource);

            if (backupHandle == nint.Zero)
            {
                throw SqliteInteropException.CreateException(
                    (SqliteResult)Sqlite3Native.sqlite3_errcode(destinationHandle),
                    destinationHandle,
                    "SQLite backup init");
            }

            return new Sqlite3Backup(new Sqlite3BackupHandle(backupHandle));
        }
    }

    public SqliteResult Step(int pages = -1)
    {
        ThrowIfInvalid();
        return (SqliteResult)Sqlite3Native.sqlite3_backup_step(_handle.DangerousGetHandle(), pages);
    }

    public int Remaining()
    {
        ThrowIfInvalid();
        return Sqlite3Native.sqlite3_backup_remaining(_handle.DangerousGetHandle());
    }

    public int PageCount()
    {
        ThrowIfInvalid();
        return Sqlite3Native.sqlite3_backup_pagecount(_handle.DangerousGetHandle());
    }

    public SqliteResult Finish()
    {
        if (_handle.IsClosed || _handle.IsInvalid)
        {
            return SqliteResult.OK;
        }

        SqliteResult result = (SqliteResult)Sqlite3Native.sqlite3_backup_finish(_handle.DangerousGetHandle());
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
