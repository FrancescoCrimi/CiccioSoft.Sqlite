// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace CiccioSoft.Sqlite.Native;

/// <summary>
/// Represents an error returned by the native SQLite interop layer.
/// </summary>
public sealed unsafe class Exception : System.Exception
{
    private Exception(string message, ResultCode resultCode, string errorString, string errorMessage)
        : base(message)
    {
        ResultCode = resultCode;
        BaseResultCode = resultCode.ToPrimary();
        ErrorString = errorString;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Gets the extended SQLite error code.
    /// </summary>
    public ResultCode ResultCode { get; }

    /// <summary>
    /// Gets the base SQLite error code (lowest 8 bits).
    /// </summary>
    public ResultCode BaseResultCode { get; }

    /// <summary>
    /// Gets the generic English-language description of the result code,
    /// as returned by <c>sqlite3_errstr</c>. This value is always available
    /// regardless of whether a database connection handle exists.
    /// </summary>
    /// <remarks>
    /// Examples: "not an error", "SQL logic error", "database is locked",
    /// "constraint failed", "disk I/O error".
    /// </remarks>
    public string? ErrorString { get; }

    /// <summary>
    /// Gets the connection-specific native message returned by <c>sqlite3_errmsg</c>.
    /// When no valid connection handle was available at construction time,
    /// this falls back to <see cref="ErrorString"/>.
    /// </summary>
    public string? ErrorMessage { get; }

    internal static Exception ReturnException(ResultCode resultCode, string errorMessage, string caller)
    {
        string errorString = Connection.ErrorString(resultCode);

        if (errorMessage is null || errorMessage == "")
        {
            errorMessage = errorString;
        }

        string message =
            $"{caller} failed. " +
            $"Error: {errorString}, " +
            $"PrimaryResultCode: {resultCode.ToPrimary()}, " +
            $"ResultCode: {resultCode}, " +
            $"Message: {errorMessage}";

        return new Exception(message, resultCode, errorString, errorMessage);
    }

    [DoesNotReturn]
    internal static void ThrowException(ResultCode resultCode, string errorMessage, string caller)
    {
        throw ReturnException(resultCode, errorMessage, caller);
    }
}
