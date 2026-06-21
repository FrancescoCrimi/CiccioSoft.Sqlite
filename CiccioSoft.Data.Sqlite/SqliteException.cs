// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Data.Common;
using CiccioSoft.Sqlite.Interop;

namespace CiccioSoft.Data.Sqlite;

/// <summary>
///     Represents a SQLite error.
/// </summary>
public class SqliteException : DbException
{
    public SqliteException(string message, SqliteInteropException? innerException = null)
    : base(message, innerException)
    {
        SqliteErrorCode = innerException?.BaseErrorCode;
        SqliteExtendedErrorCode = innerException?.ExtendedErrorCode;
    }

    /// <summary>
    ///     Gets the SQLite error code.
    /// </summary>
    /// <value>The SQLite error code.</value>
    public virtual SqliteResult? SqliteErrorCode { get; }

    /// <summary>
    ///     Gets the extended SQLite error code.
    /// </summary>
    /// <value>The SQLite error code.</value>
    public virtual SqliteExtendedErrorCode? SqliteExtendedErrorCode { get; }
}
