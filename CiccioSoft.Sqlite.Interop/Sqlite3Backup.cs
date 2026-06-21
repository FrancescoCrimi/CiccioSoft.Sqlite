// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using Microsoft.Win32.SafeHandles;
using CiccioSoft.Sqlite.Interop.Native;
using System.Text;
using System.Buffers;

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

    public static Sqlite3Backup InitBackup(Sqlite3 destination, string destinationDatabaseName, Sqlite3 source, string sourceDatabaseName = "main")
    {
        ArgumentNullException.ThrowIfNull(destination);
        // ThrowIfInvalid();
        if (destination.Handle.IsInvalid) throw new ObjectDisposedException(nameof(Sqlite3));

        ArgumentNullException.ThrowIfNull(source);
        // source.ThrowIfInvalid();
        if (source.Handle.IsInvalid) throw new ObjectDisposedException(nameof(Sqlite3));

        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDatabaseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDatabaseName);

        int destinationBytes = Encoding.UTF8.GetByteCount(destinationDatabaseName);
        int sourceBytes = Encoding.UTF8.GetByteCount(sourceDatabaseName);
        int destinationTotalNeeded = destinationBytes + 1;
        int sourceTotalNeeded = sourceBytes + 1;
        int totalNeeded = destinationTotalNeeded + sourceTotalNeeded;

        byte[]? pooled = null;
        Span<byte> buffer = totalNeeded <= 256
            ? stackalloc byte[totalNeeded]
            : (pooled = ArrayPool<byte>.Shared.Rent(totalNeeded)).AsSpan(0, totalNeeded);

        try
        {
            Span<byte> destinationBuffer = buffer[..destinationTotalNeeded];
            Span<byte> sourceBuffer = buffer.Slice(destinationTotalNeeded, sourceTotalNeeded);

            Encoding.UTF8.GetBytes(destinationDatabaseName, destinationBuffer);
            destinationBuffer[destinationBytes] = 0;

            Encoding.UTF8.GetBytes(sourceDatabaseName, sourceBuffer);
            sourceBuffer[sourceBytes] = 0;

            fixed (byte* pDest = destinationBuffer)
            fixed (byte* pSource = sourceBuffer)
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
        finally
        {
            if (pooled != null)
                ArrayPool<byte>.Shared.Return(pooled);
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
