// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using SQLitePCL;

namespace CiccioSoft.Sqlite.Native.Benchmark;

public class WriteSpan
{
    // public const string DbFile = ":memory:";
    private readonly string DbFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "write.db");
    private const int RowCount = 100_000; // Ridotto a 100k perché BenchmarkDotNet esegue i test molte volte
    private static ReadOnlySpan<byte> TestString => "User_Performance_Test_String_12345"u8;

    private sqlite3 _dbPCLRaw;
    private Connection _dbCiccioSoft;


    [GlobalSetup(Target = nameof(WriteSpan_PCLRaw))]
    public void GlobalSetup_PCLRaw()
    {
        Batteries_V2.Init();
        raw.sqlite3_open(DbFile, out _dbPCLRaw); // Usiamo :memory: per non subire l'I/O del disco
        raw.sqlite3_exec(_dbPCLRaw, "PRAGMA journal_mode = WAL;");
        raw.sqlite3_exec(_dbPCLRaw, "PRAGMA synchronous = OFF;");
    }

    [GlobalCleanup(Target = nameof(WriteSpan_PCLRaw))]
    public void GlobalCleanup_PCLRaw() => raw.sqlite3_close(_dbPCLRaw);

    [IterationSetup(Target = nameof(WriteSpan_PCLRaw))]
    public void IterationSetup_PCLRaw()
    {
        raw.sqlite3_exec(_dbPCLRaw, "DROP TABLE IF EXISTS Users;");
        raw.sqlite3_exec(_dbPCLRaw, "CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
    }

    [Benchmark(Baseline = true)] // Imposta SQLitePCLRaw come punto di riferimento
    public void WriteSpan_PCLRaw()
    {
        raw.sqlite3_exec(_dbPCLRaw, "BEGIN;");
        raw.sqlite3_prepare_v2(_dbPCLRaw, "INSERT INTO Users VALUES (?, ?, ?);", out sqlite3_stmt stmtRaw);
        for (int i = 0; i < RowCount; i++)
        {
            raw.sqlite3_reset(stmtRaw);
            raw.sqlite3_bind_int64(stmtRaw, 1, i);
            raw.sqlite3_bind_text(stmtRaw, 2, TestString);
            raw.sqlite3_bind_double(stmtRaw, 3, i * 1.1);
            raw.sqlite3_step(stmtRaw);
        }
        raw.sqlite3_exec(_dbPCLRaw, "COMMIT;");
    }


    [GlobalSetup(Target = nameof(WriteSpan_CiccioSoft))]
    public void GlobalSetup_CiccioSoft()
    {
        NativeLibraryResolver.Configure(NativeSource.SourceGear);
        _dbCiccioSoft = Connection.Open(DbFile, OpenFlags.ReadWrite | OpenFlags.Create);
        _dbCiccioSoft.Execute("PRAGMA journal_mode = WAL;");
        _dbCiccioSoft.Execute("PRAGMA synchronous = OFF;");
    }

    [GlobalCleanup(Target = nameof(WriteSpan_CiccioSoft))]
    public void GlobalCleanup_CiccioSoft() => _dbCiccioSoft?.Dispose();

    [IterationSetup(Target = nameof(WriteSpan_CiccioSoft))]
    public void IterationSetup_CiccioSoft()
    {
        if (_dbCiccioSoft != null)
        {
            _dbCiccioSoft.Execute("DROP TABLE IF EXISTS Users;");
            _dbCiccioSoft.Execute("CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
        }
    }

    [Benchmark]
    public void WriteSpan_CiccioSoft()
    {
        if (_dbCiccioSoft != null)
        {
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
                _dbCiccioSoft.Execute("COMMIT;");
            }
        }
    }
}
