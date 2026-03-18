// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

namespace CiccioSoft.Data.Sqlite;

/// <summary>
/// Specifies the checkpoint mode used by <see cref="SqliteConnection.Checkpoint(SqliteWalCheckpointMode, System.Threading.CancellationToken)"/>.
/// </summary>
public enum SqliteWalCheckpointMode
{
    /// <summary>
    /// Checkpoint as many frames as possible without waiting for readers.
    /// </summary>
    Passive = 0,

    /// <summary>
    /// Wait for readers before moving all frames from WAL to the main database.
    /// </summary>
    Full = 1,

    /// <summary>
    /// Like <see cref="Full"/>, then attempts to shrink the WAL file.
    /// </summary>
    Restart = 2,

    /// <summary>
    /// Like <see cref="Restart"/>, and truncates the WAL file to zero bytes if possible.
    /// </summary>
    Truncate = 3,
}
