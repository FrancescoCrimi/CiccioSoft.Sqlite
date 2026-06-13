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
/// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/database-errors">Database Errors</seealso>
public class SqliteException : DbException
{
    // public SqliteException(string message, int baseCode = 0, int extendedCode = 0, Exception? innerException = null)
    //     : base(message, innerException)
    // {
    //     SqliteErrorCode = baseCode;
    //     SqliteExtendedErrorCode = extendedCode;
    // }

    public SqliteException(string message, SqliteInteropException? innerException = null)
    : base(message, innerException)
    {
        SqliteErrorCode = innerException.BaseErrorCode;
        SqliteExtendedErrorCode = innerException.ExtendedErrorCode;
    }

    /// <summary>
    ///     Gets the SQLite error code.
    /// </summary>
    /// <value>The SQLite error code.</value>
    /// <seealso href="https://www.sqlite.org/rescode.html">SQLite Result Codes</seealso>
    public virtual SqliteResult SqliteErrorCode { get; }

    /// <summary>
    ///     Gets the extended SQLite error code.
    /// </summary>
    /// <value>The SQLite error code.</value>
    /// <seealso href="https://www.sqlite.org/rescode.html#extrc">SQLite Result Codes</seealso>
    public virtual SqliteExtendedErrorCode SqliteExtendedErrorCode { get; }
}
