// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace CiccioSoft.Data.Sqlite.Benchmark;

// ==========================================
// BENCHMARK DI LETTURA (SELECT)
// ==========================================

public class ReadString
{
    private const int RowCount = 100_000;
    private const string connectionString = @"Data Source=C:\Users\franc\Dev\CiccioSoft.Sqlite\read.db";
    // private const string connectionString = ":memory:";
    private const string TestString = "User_Performance_Test_String_12345";

    private readonly Consumer _consumer = new Consumer();


    #region Microsoft.Data.Sqlite

    [GlobalSetup(Target = nameof(ReadString_Microsoft))]
    public void Setup_Microsoft()
    {
        // 1. Creazione ed apertura di Connection
        using var _db1 = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
        _db1.Open();

        // 2. Creazione di Command
        using var command = _db1.CreateCommand();

        // 3. Esecuzione DDL senza transazione
        command.CommandText = "PRAGMA synchronous = OFF";
        command.ExecuteNonQuery();
        command.CommandText = "DROP TABLE IF EXISTS Users";
        command.ExecuteNonQuery();
        command.CommandText = "CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL)";
        command.ExecuteNonQuery();

        // 4. Inizio della transazione
        using var transaction = _db1.BeginTransaction(); // FONDAMENTALE per le prestazioni in SQLite

        // 5. Configurazione di Command per l'inserimento massivo
        command.CommandText = "INSERT INTO Users (Id, Name, Score) VALUES ($id, $name, $score)";
        command.Transaction = transaction;

        // 6. Creazione e aggiunta parametri tipizzati
        // var idParam = command.Parameters.Add("$id", Microsoft.Data.Sqlite.SqliteType.Integer);
        // var nameParam = command.Parameters.Add("$name", Microsoft.Data.Sqlite.SqliteType.Text);
        // var scoreParam = command.Parameters.Add("$score", Microsoft.Data.Sqlite.SqliteType.Real);
        var idParam = new Microsoft.Data.Sqlite.SqliteParameter("$id", Microsoft.Data.Sqlite.SqliteType.Text);
        var nameParam = new Microsoft.Data.Sqlite.SqliteParameter("$name", Microsoft.Data.Sqlite.SqliteType.Text);
        var scoreParam = new Microsoft.Data.Sqlite.SqliteParameter("$score", Microsoft.Data.Sqlite.SqliteType.Integer);
        command.Parameters.Add(idParam);
        command.Parameters.Add(nameParam);
        command.Parameters.Add(scoreParam);

        // 7. Preparazione di Command, compila lo statement nativo SQLite in memoria (sqlite3_prepare_v2) ?? Da verificare ??
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

    [Benchmark(Baseline = true)] // Imposta Microsoft come punto di riferimento
    public unsafe void ReadString_Microsoft()
    {
        using var _db1 = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
        _db1.Open();

        // // Per le massime performance in lettura su SQLite, usa la modalità Deferred o ReadOnly se supportata
        // using var transaction = _db1.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

        using var command = _db1.CreateCommand();
        command.CommandText = "SELECT Id, Name, Score FROM Users";
        // command.Transaction = transaction;

        // CommandBehavior.SequentialAccess ottimizza la memoria (ideale per milioni di righe)
        using var reader = command.ExecuteReader(System.Data.CommandBehavior.SequentialAccess);

        while (reader.Read())
        {
            long id = reader.GetInt64(0);
            string name = reader.GetString(1);
            double score = reader.GetFloat(2);
            _consumer.Consume(id);
            _consumer.Consume(name);
            _consumer.Consume(score);
        }

        // // Nota: Nelle letture non si fa il Commit(). Il Rollback() o la chiusura del blocco 'using'
        // // rilascia immediatamente tutti i lock senza alterare il database.
        // transaction.Rollback();
    }

    #endregion


    #region CiccioSoft.Data.Sqlite

    [GlobalSetup(Target = nameof(ReadString_CiccioSoft))]
    public void Setup_CiccioSoft()
    {
        CiccioSoft.Sqlite.NativeLibraryResolver.Configure(CiccioSoft.Sqlite.NativeSource.SourceGear);

        using var _db2 = new CiccioSoft.Data.Sqlite.SqliteConnection(connectionString);
        _db2.Open();

        using var command = _db2.CreateCommand();

        command.CommandText = "PRAGMA synchronous = OFF;";
        command.ExecuteNonQuery();
        command.CommandText = "DROP TABLE IF EXISTS Users;";
        command.ExecuteNonQuery();
        command.CommandText = "CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);";
        command.ExecuteNonQuery();

        using var transaction = _db2.BeginTransaction(); // FONDAMENTALE per le prestazioni in SQLite

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

    [Benchmark]
    public void ReadString_CiccioSoft()
    {
        using var _db2 = new CiccioSoft.Data.Sqlite.SqliteConnection(connectionString);
        _db2.Open();
        using var command = _db2.CreateCommand();
        command.CommandText = "SELECT Id, Name, Score FROM Users";

        using var reader = command.ExecuteReader(System.Data.CommandBehavior.SequentialAccess);
        while (reader.Read())
        {
            long id = reader.GetInt64(0);
            string name = reader.GetString(1);
            double score = reader.GetFloat(2);
            _consumer.Consume(id);
            _consumer.Consume(name);
            _consumer.Consume(score);
        }
    }

    #endregion
}
