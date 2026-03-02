using System;
using System.IO;
using CiccioSoft.Data.Sqlite;
using Xunit;

namespace CiccioSoft.Data.Sqlite.Tests;

public class SqliteConnectionProfileTest
{
    [Fact]
    public void Profile_DefaultsToDefaultMode()
    {
        var builder = new SqliteConnectionStringBuilder();

        Assert.Equal(SqliteConnectionProfile.Default, builder.Profile);
    }

    [Fact]
    public void Profile_CanParseFromConnectionString()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            ConnectionString = "Data Source=:memory:;Profile=StrictSingleConnection"
        };

        Assert.Equal(SqliteConnectionProfile.StrictSingleConnection, builder.Profile);
    }

    [Fact]
    public void Open_DefaultProfileForcesWalAndForeignKeys()
    {
        using var connection = new SqliteConnection("Data Source=:memory:;");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode;";
        var journal = (command.ExecuteScalar() as string) ?? string.Empty;

        command.CommandText = "PRAGMA foreign_keys;";
        var foreignKeys = Convert.ToInt32(command.ExecuteScalar());

        Assert.Equal("wal", journal.ToLowerInvariant());
        Assert.Equal(1, foreignKeys);
    }

    [Fact]
    public void Open_StrictSingleConnectionForcesDeleteJournalAndNoBusyTimeout()
    {
        string path = Path.Combine(Path.GetTempPath(), $"strict-profile-{Guid.NewGuid():N}.db");
        try
        {
            using var connection = new SqliteConnection($"Data Source={path};Profile=StrictSingleConnection");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA journal_mode;";
            var journal = (command.ExecuteScalar() as string) ?? string.Empty;

            command.CommandText = "PRAGMA busy_timeout;";
            var busyTimeout = Convert.ToInt32(command.ExecuteScalar());

            Assert.Equal("delete", journal.ToLowerInvariant());
            Assert.Equal(0, busyTimeout);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
