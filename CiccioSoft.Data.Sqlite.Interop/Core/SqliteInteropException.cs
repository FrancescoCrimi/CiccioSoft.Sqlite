// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;

namespace CiccioSoft.Data.Sqlite.Interop;

/// <summary>
/// Represents an error returned by the native SQLite interop layer.
/// </summary>
public sealed class SqliteInteropException : Exception
{
    public SqliteInteropException(
        string message,
        SqliteResult baseErrorCode,
        SqliteExtendedErrorCode extendedErrorCode,
        string nativeMessage)
        : base(message)
    {
        BaseErrorCode = baseErrorCode;
        ExtendedErrorCode = extendedErrorCode;
        NativeMessage = nativeMessage;
        ExtendedErrorDescription = SqliteErrorHelper.GetExtendedErrorDescription(extendedErrorCode);
    }

    /// <summary>
    /// Gets the base SQLite error code (lowest 8 bits).
    /// </summary>
    public SqliteResult BaseErrorCode { get; }

    /// <summary>
    /// Gets the extended SQLite error code.
    /// </summary>
    public SqliteExtendedErrorCode ExtendedErrorCode { get; }

    /// <summary>
    /// Gets a human-readable message for the extended SQLite error code.
    /// </summary>
    public string ExtendedErrorDescription { get; }

    /// <summary>
    /// Gets the native message returned by SQLite.
    /// </summary>
    public string NativeMessage { get; }
}
