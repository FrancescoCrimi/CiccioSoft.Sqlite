// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using SQLitePCL;

namespace CiccioSoft.Sqlite.Benchmark;

public class ReadString
{
    // private const string DbFile = ":memory:";
    private readonly string DbFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "read.db");
    private const int RowCount = 100_000;
    private const string TestString = "User_Performance_Test_String_12345";

    private sqlite3 _dbPCLRaw;
    private CiccioSoft.Sqlite.SqliteConnection _dbCiccioSoft;

    // Il Consumer dice a BenchmarkDotNet di consumare il valore per evitare ottimizzazioni aggressive del JIT/AOT
    private readonly Consumer _consumer = new Consumer();


    [GlobalSetup(Target = nameof(ReadString_PCLRaw))]
    public void Setup_PCLRaw()
    {
        Batteries_V2.Init();
        raw.sqlite3_open(DbFile, out _dbPCLRaw);
        raw.sqlite3_exec(_dbPCLRaw, "PRAGMA synchronous = OFF;");
        raw.sqlite3_exec(_dbPCLRaw, "DROP TABLE IF EXISTS Users;");
        raw.sqlite3_exec(_dbPCLRaw, "CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
        raw.sqlite3_exec(_dbPCLRaw, "BEGIN;");
        raw.sqlite3_prepare_v2(_dbPCLRaw, "INSERT INTO Users VALUES (?, ?, ?);", out var stmt);
        for (int i = 0; i < RowCount; i++)
        {
            raw.sqlite3_reset(stmt);
            raw.sqlite3_bind_int64(stmt, 1, i);
            raw.sqlite3_bind_text(stmt, 2, TestString);
            raw.sqlite3_bind_double(stmt, 3, i * 1.1);
            raw.sqlite3_step(stmt);
        }
        raw.sqlite3_exec(_dbPCLRaw, "COMMIT;");
    }

    [GlobalCleanup(Target = nameof(ReadString_PCLRaw))]
    public void Cleanup_PCLRaw() => raw.sqlite3_close_v2(_dbPCLRaw);

    [Benchmark(Baseline = true)] // Imposta SQLitePCLRaw come punto di riferimento
    public void ReadString_PCLRaw()
    {
        raw.sqlite3_prepare_v2(_dbPCLRaw, "SELECT Id, Name, Score FROM Users;", out var stmtRaw);
        while (raw.sqlite3_step(stmtRaw) == SQLitePCL.raw.SQLITE_ROW)
        {
            long id = raw.sqlite3_column_int64(stmtRaw, 0);
            string name = raw.sqlite3_column_text(stmtRaw, 1).utf8_to_string();
            double score = raw.sqlite3_column_double(stmtRaw, 2);
            _consumer.Consume(id);
            _consumer.Consume(name);
            _consumer.Consume(score);
        }
        raw.sqlite3_finalize(stmtRaw);
    }


    [GlobalSetup(Target = nameof(ReadString_CiccioSoft))]
    public void Setup_CiccioSoft()
    {
        CiccioSoft.Sqlite.Native.NativeLibraryResolver.Configure(CiccioSoft.Sqlite.Native.NativeSource.SourceGear);
        var option = new SqliteConnectionOptions
        {
            DataSource = DbFile,
            AdditionalFlags = OpenFlagsDefaults.Coordinated,
            ConcurrencyMode = SqliteConcurrencyMode.Native
        };
        _dbCiccioSoft = new CiccioSoft.Sqlite.SqliteConnection(option);
        _dbCiccioSoft.Open();
        _dbCiccioSoft.Execute("PRAGMA synchronous = OFF;");
        _dbCiccioSoft.Execute("DROP TABLE IF EXISTS Users;");
        _dbCiccioSoft.Execute("CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
        _dbCiccioSoft.Execute("BEGIN;");
        using (var stmt = _dbCiccioSoft.Prepare("INSERT INTO Users VALUES (?, ?, ?);"))
        {
            for (int i = 0; i < RowCount; i++)
            {
                stmt.Reset();
                stmt.BindLong(1, i);
                stmt.BindText(2, TestString);
                stmt.BindDouble(3, i * 1.1);
                stmt.Step();
            }
        }
        _dbCiccioSoft.Execute("COMMIT;");
    }

    [GlobalCleanup(Target = nameof(ReadString_CiccioSoft))]
    public void Cleanup_CiccioSoft() => _dbCiccioSoft?.Dispose();

    [Benchmark]
    public void ReadString_CiccioSoft()
    {
        using (var stmt = _dbCiccioSoft.Prepare("SELECT Id, Name, Score FROM Users;"))
        {
            while (stmt.Step())
            {
                long id = stmt.GetLong(0);
                string name = stmt.GetText(1);
                double score = stmt.GetDouble(2);
                _consumer.Consume(id);
                _consumer.Consume(name);
                _consumer.Consume(score);
            }
        }
    }
}
