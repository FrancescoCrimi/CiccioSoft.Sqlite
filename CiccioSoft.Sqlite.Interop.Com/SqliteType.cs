// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using CiccioSoft.Sqlite.Interop.Native;

namespace CiccioSoft.Sqlite.Interop.Com;

/// <summary>
///     Represents the type affinities used by columns in SQLite tables.
/// </summary>
public enum SqliteType
{
    /// <summary>
    ///     A signed integer.
    /// </summary>
    Integer = Sqlite3Native.SQLITE_INTEGER,

    /// <summary>
    ///     A floating point value.
    /// </summary>
    Real = Sqlite3Native.SQLITE_FLOAT,

    /// <summary>
    ///     A text string.
    /// </summary>
    Text = Sqlite3Native.SQLITE_TEXT,

    /// <summary>
    ///     A blob of data.
    /// </summary>
    Blob = Sqlite3Native.SQLITE_BLOB,

    Null = Sqlite3Native.SQLITE_NULL
}
