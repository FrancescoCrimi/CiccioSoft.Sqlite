// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using SQLitePCL;

namespace CiccioSoft.Sqlite.Native.Benchmark;

public class ReadSpan
{
    public const int RowCount = 100_000;
    public const string TestString = "User_Performance_Test_String_12345";
    public const string DbFile = @"C:\Users\franc\Dev\CiccioSoft.Sqlite\read.db";
    // public const string DbFile = ":memory:";

    private sqlite3 _dbPCLRaw;
    private Connection _dbCiccioSoft;

    // Il Consumer dice a BenchmarkDotNet di consumare il valore per evitare ottimizzazioni aggressive del JIT/AOT
    private readonly Consumer _consumer = new Consumer();

    // ==========================================
    // BENCHMARK DI LETTURA (SELECT)
    // ==========================================

    [GlobalSetup(Target = nameof(ReadSpan_PCLRaw))]
    public void Setup_PCLRaw()
    {
        Batteries_V2.Init();
        raw.sqlite3_open(DbFile, out _dbPCLRaw);
        raw.sqlite3_exec(_dbPCLRaw, "PRAGMA synchronous = OFF;");
        raw.sqlite3_exec(_dbPCLRaw, "DROP TABLE IF EXISTS Users;");
        raw.sqlite3_exec(_dbPCLRaw, "CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
        raw.sqlite3_exec(_dbPCLRaw, "BEGIN;");
        raw.sqlite3_prepare_v2(_dbPCLRaw, "INSERT INTO Users VALUES (?, ?, ?);", out var stmt);
        using (stmt)
        {
            for (int i = 0; i < RowCount; i++)
            {
                raw.sqlite3_reset(stmt);
                raw.sqlite3_bind_int64(stmt, 1, i);
                raw.sqlite3_bind_text(stmt, 2, TestString);
                raw.sqlite3_bind_double(stmt, 3, i * 1.1);
                raw.sqlite3_step(stmt);
            }
        }
        raw.sqlite3_exec(_dbPCLRaw, "COMMIT;");
    }

    [GlobalCleanup(Target = nameof(ReadSpan_PCLRaw))]
    public void Cleanup_PCLRaw() => raw.sqlite3_close_v2(_dbPCLRaw);

    [Benchmark(Baseline = true)] // Imposta SQLitePCLRaw come punto di riferimento
    public void ReadSpan_PCLRaw()
    {
        raw.sqlite3_prepare_v2(_dbPCLRaw, "SELECT Id, Name, Score FROM Users;", out var stmtRaw);
        using (stmtRaw)
        {
            while (raw.sqlite3_step(stmtRaw) == SQLitePCL.raw.SQLITE_ROW)
            {
                long id = raw.sqlite3_column_int64(stmtRaw, 0);
                ReadOnlySpan<byte> nameSpan = raw.sqlite3_column_blob(stmtRaw, 1);
                double score = raw.sqlite3_column_double(stmtRaw, 2);
                _consumer.Consume(id);
                _consumer.Consume(nameSpan[0]);
                _consumer.Consume(score);
            }
        }
    }



    [GlobalSetup(Target = nameof(ReadSpan_CiccioSoft))]
    public void Setup_CiccioSoft()
    {
        NativeLibraryResolver.Configure(NativeSource.SourceGear);
        _dbCiccioSoft = Connection.Open(DbFile, OpenFlags.ReadWrite | OpenFlags.Create);
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

    [GlobalCleanup(Target = nameof(ReadSpan_CiccioSoft))]
    public void Cleanup_CiccioSoft() => _dbCiccioSoft?.Dispose();

    [Benchmark]
    public void ReadSpan_CiccioSoft()
    {
        if (_dbCiccioSoft != null)
        {
            using (var stmt = _dbCiccioSoft.Prepare("SELECT Id, Name, Score FROM Users;"))
            {
                while (stmt.Step())
                {
                    long id = stmt.GetLong(0);
                    ReadOnlySpan<byte> nameSpan = stmt.GetTextAsSpan(1);
                    double score = stmt.GetDouble(2);
                    _consumer.Consume(id);
                    _consumer.Consume(nameSpan[0]);
                    _consumer.Consume(score);
                }
            }
        }
    }
}
