// Copyright (c) 2026 Francesco Crimi
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.IO;
using CiccioSoft.Data.Sqlite;
using Xunit;

namespace CiccioSoft.Data.Sqlite.Tests.Extra;

public class SqliteConnectionSettingsTests
{
    // [Fact]
    // public void Open_DoesNotForceForeignKeysOrJournalModeByDefault()
    // {
    //     using var connection = new SqliteConnection("Data Source=:memory:;");
    //     connection.Open();

    //     using var command = connection.CreateCommand();
    //     command.CommandText = "PRAGMA busy_timeout;";
    //     var busyTimeout = Convert.ToInt32(command.ExecuteScalar());

    //     command.CommandText = "PRAGMA foreign_keys;";
    //     var foreignKeys = Convert.ToInt32(command.ExecuteScalar());

    //     command.CommandText = "PRAGMA journal_mode;";
    //     var journal = (command.ExecuteScalar() as string) ?? string.Empty;

    //     Assert.Equal(30000, busyTimeout);
    //     Assert.InRange(foreignKeys, 0, 1);
    //     Assert.Equal("memory", journal.ToLowerInvariant());
    // }

    // [Fact]
    // public void Open_AppliesExplicitConnectionSettings()
    // {
    //     using var connection = new SqliteConnection("Data Source=:memory:;Busy Timeout=1234;Foreign Keys=True;Journal Mode=MEMORY;Recursive Triggers=True");
    //     connection.Open();

    //     using var command = connection.CreateCommand();
    //     command.CommandText = "PRAGMA busy_timeout;";
    //     var busyTimeout = Convert.ToInt32(command.ExecuteScalar());

    //     command.CommandText = "PRAGMA foreign_keys;";
    //     var foreignKeys = Convert.ToInt32(command.ExecuteScalar());

    //     command.CommandText = "PRAGMA journal_mode;";
    //     var journal = (command.ExecuteScalar() as string) ?? string.Empty;

    //     command.CommandText = "PRAGMA recursive_triggers;";
    //     var recursiveTriggers = Convert.ToInt32(command.ExecuteScalar());

    //     Assert.Equal(1234, busyTimeout);
    //     Assert.Equal(1, foreignKeys);
    //     Assert.Equal("memory", journal.ToLowerInvariant());
    //     Assert.Equal(1, recursiveTriggers);
    // }

    // [Fact]
    // public void Open_AppliesPragmaAliasConnectionSettings()
    // {
    //     string path = Path.Combine(Path.GetTempPath(), $"settings-alias-{Guid.NewGuid():N}.db");
    //     try
    //     {
    //         using var connection = new SqliteConnection($"Data Source={path};Pooling=False;busy_timeout=2500;foreign_keys=0;journal_mode=WAL;recursive_triggers=0");
    //         connection.Open();

    //         using var command = connection.CreateCommand();
    //         command.CommandText = "PRAGMA busy_timeout;";
    //         var busyTimeout = Convert.ToInt32(command.ExecuteScalar());

    //         command.CommandText = "PRAGMA foreign_keys;";
    //         var foreignKeys = Convert.ToInt32(command.ExecuteScalar());

    //         command.CommandText = "PRAGMA journal_mode;";
    //         var journal = (command.ExecuteScalar() as string) ?? string.Empty;

    //         command.CommandText = "PRAGMA recursive_triggers;";
    //         var recursiveTriggers = Convert.ToInt32(command.ExecuteScalar());

    //         Assert.Equal(2500, busyTimeout);
    //         Assert.Equal(0, foreignKeys);
    //         Assert.Equal("wal", journal.ToLowerInvariant());
    //         Assert.Equal(0, recursiveTriggers);
    //     }
    //     finally
    //     {
    //         if (File.Exists(path))
    //         {
    //             File.Delete(path);
    //         }
    //     }
    // }
}
