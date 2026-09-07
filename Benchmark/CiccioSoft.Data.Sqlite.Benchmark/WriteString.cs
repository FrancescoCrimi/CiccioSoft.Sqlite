// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using BenchmarkDotNet.Attributes;

namespace CiccioSoft.Data.Sqlite.Benchmark;

// ==========================================
// BENCHMARK DI SCRITTURA (INSERT INTO)
// ==========================================

public class WriteString
{
    private const int RowCount = 100_000; // Ridotto a 100k perché BenchmarkDotNet esegue i test molte volte
    private const string connectionString = @"Data Source=C:\Users\franc\Dev\CiccioSoft.Sqlite\write.db";
    // private const string connectionString = ":memory:";
    private const string TestString = "User_Performance_Test_String_12345";

    [GlobalSetup]
    public void GlobalSetup()
    {
        CiccioSoft.Sqlite.NativeLibraryResolver.Configure(CiccioSoft.Sqlite.NativeSource.SourceGear);
    }

    #region Microsoft.Data.Sqlite

    [IterationSetup(Target = nameof(WriteString_Microsoft))]
    public void IterationSetup_Microsoft()
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "DROP TABLE IF EXISTS Users";
        command.ExecuteNonQuery();
        command.CommandText = "CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL)";
        command.ExecuteNonQuery();
    }

    [Benchmark(Baseline = true)] // Imposta Microsoft come punto di riferimento
    public void WriteString_Microsoft()
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction(); // FONDAMENTALE per le prestazioni in SQLite

        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Users (Id, Name, Score) VALUES ($id, $name, $score)";
        command.Transaction = transaction;

        var idParam = command.Parameters.Add("$id", Microsoft.Data.Sqlite.SqliteType.Integer);
        var nameParam = command.Parameters.Add("$name", Microsoft.Data.Sqlite.SqliteType.Text);
        var scoreParam = command.Parameters.Add("$score", Microsoft.Data.Sqlite.SqliteType.Real);

        command.Prepare();

        try
        {
            for (int i = 0; i < RowCount; i++)
            {
                idParam.Value = i;
                nameParam.Value = TestString;
                scoreParam.Value = i * 1.1;
                command.ExecuteNonQuery();
            }
            transaction.Commit();
        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }
    }

    #endregion


    #region CiccioSoft.Data.Sqlite

    [IterationSetup(Target = nameof(WriteString_CiccioSoft))]
    public void IterationSetup_CiccioSoft()
    {
        using var connection = new CiccioSoft.Data.Sqlite.SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();

        command.CommandText = "DROP TABLE IF EXISTS Users";
        command.ExecuteNonQuery();
        command.CommandText = "CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL)";
        command.ExecuteNonQuery();
    }

    [Benchmark]
    public void WriteString_CiccioSoft()
    {
        using var connection = new CiccioSoft.Data.Sqlite.SqliteConnection(connectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction(); // FONDAMENTALE per le prestazioni in SQLite

        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Users (Id, Name, Score) VALUES ($id, $name, $score)";
        command.Transaction = transaction;

        var idParam = command.Parameters.Add("$id", CiccioSoft.Sqlite.SqliteType.Integer);
        var nameParam = command.Parameters.Add("$name", CiccioSoft.Sqlite.SqliteType.Text);
        var scoreParam = command.Parameters.Add("$score", CiccioSoft.Sqlite.SqliteType.Real);

        command.Prepare();

        try
        {
            for (int i = 0; i < RowCount; i++)
            {
                idParam.Value = i;
                nameParam.Value = TestString;
                scoreParam.Value = i * 1.1;
                command.ExecuteNonQuery();
            }
            transaction.Commit();
        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }
    }

    #endregion
}
