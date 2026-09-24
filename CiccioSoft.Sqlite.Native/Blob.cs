// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CiccioSoft.Sqlite.Native.Interop;

namespace CiccioSoft.Sqlite.Native;

/// <summary>
/// Provides low-allocation, incremental read/write access to a single BLOB value
/// stored in a table row, via the <c>sqlite3_blob_*</c> incremental I/O API.
/// </summary>
/// <threadsafety>
/// This class is not inherently thread-safe, consistent with the rest of the library.
/// </threadsafety>
public sealed unsafe class Blob : SafeHandle
{
    private readonly Connection _connection;

    #region Ctor and safehandle

    internal Blob(sqlite3_blob* pBlob, Connection connection)
        : base((nint)pBlob, true)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
    }

    public override bool IsInvalid => handle == nint.Zero;

    protected override bool ReleaseHandle()
    {
        _ = NativeMethods.sqlite3_blob_close((sqlite3_blob*)handle);
        return true;
    }

    private sqlite3_blob* Sqlite3BlobHandle => (sqlite3_blob*)DangerousGetHandle();

    #endregion


    #region Blob

    /// <summary>
    /// Returns the size in bytes of the BLOB accessible via this handle.
    /// </summary>
    public int Bytes()
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        int rtn = NativeMethods.sqlite3_blob_bytes(Sqlite3BlobHandle);
        return rtn;
    }

    /// <summary>
    /// Reads data from the BLOB starting at the given byte offset into the destination span.
    /// </summary>
    /// <param name="destination">The buffer to fill; its length determines how many bytes are read.</param>
    /// <param name="blobOffset">The zero-based byte offset within the BLOB to start reading from.</param>
    /// <exception cref="Exception">Thrown if the read fails (e.g. offset+length out of range).</exception>
    public void Read(Span<byte> destination, int blobOffset)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        if (blobOffset < 0)
            throw new ArgumentOutOfRangeException(nameof(blobOffset));

        fixed (byte* pDest = destination)
        {
            ResultCode result = (ResultCode)NativeMethods.sqlite3_blob_read(
                Sqlite3BlobHandle, pDest, destination.Length, blobOffset);

            if (result != ResultCode.OK)
                ThrowException(result, _connection.ErrorMessage());
        }
    }

    /// <summary>
    /// Writes data into the BLOB starting at the given byte offset.
    /// The BLOB must have been opened with <c>readWrite: true</c>, and the write
    /// cannot change the overall size of the BLOB (only overwrite existing bytes).
    /// </summary>
    /// <param name="source">The data to write.</param>
    /// <param name="blobOffset">The zero-based byte offset within the BLOB to start writing at.</param>
    /// <exception cref="Exception">Thrown if the write fails (e.g. read-only handle, offset out of range).</exception>
    public void Write(ReadOnlySpan<byte> source, int blobOffset)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        if (blobOffset < 0)
            throw new ArgumentOutOfRangeException(nameof(blobOffset));

        fixed (byte* pSrc = source)
        {
            ResultCode result = (ResultCode)NativeMethods.sqlite3_blob_write(
                Sqlite3BlobHandle, pSrc, source.Length, blobOffset);

            if (result != ResultCode.OK)
                ThrowException(result, _connection.ErrorMessage());
        }
    }

    /// <summary>
    /// Repositions this BLOB handle to point at the same column in a different row,
    /// avoiding the cost of closing and reopening a new handle.
    /// </summary>
    /// <param name="rowId">The rowid of the new row to point to.</param>
    /// <exception cref="Exception">Thrown if the target row/column is not found or reopen fails.</exception>
    public void Reopen(long rowId)
    {
        ObjectDisposedException.ThrowIf(IsClosed || IsInvalid, this);

        ResultCode result = (ResultCode)NativeMethods.sqlite3_blob_reopen(Sqlite3BlobHandle, rowId);

        if (result != ResultCode.OK)
            ThrowException(result, _connection.ErrorMessage());
    }

    #endregion


    #region Private Methods

    [DoesNotReturn]
    private void ThrowException(ResultCode resultCode, string errorMessage, [CallerMemberName] string caller = "")
    {
        Exception.ThrowException(resultCode, errorMessage, $"{nameof(Blob)}.{caller}");
    }

    #endregion
}
