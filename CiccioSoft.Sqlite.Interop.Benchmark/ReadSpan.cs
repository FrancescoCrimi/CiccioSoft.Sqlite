using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using SQLitePCL;
// using SQLitePCL.Ugly;

namespace CiccioSoft.Sqlite.Interop.Benchmark;

public class ReadSpan
{
    public const int RowCount = 100_000;
    public const string TestString = "User_Performance_Test_String_12345";
    public const string DbFile = @"C:\Users\franc\Dev\CiccioSoft.Sqlite\CiccioSoft.Sqlite.Interop.Benchmark\read.db";
    // public const string DbFile = ":memory:";

    private sqlite3 _db1;
    private Sqlite3 _db2;

    // Il Consumer dice a BenchmarkDotNet di consumare il valore per evitare ottimizzazioni aggressive del JIT/AOT
    private readonly Consumer _consumer = new Consumer();

    // ==========================================
    // BENCHMARK DI LETTURA (SELECT)
    // ==========================================

    [GlobalSetup(Target = nameof(ReadSpan_SQLitePCL))]
    public void Setup_SQLitePCL()
    {
        Batteries_V2.Init();
        raw.sqlite3_open(DbFile, out _db1);
        raw.sqlite3_exec(_db1, "PRAGMA synchronous = OFF;");
        raw.sqlite3_exec(_db1, "DROP TABLE IF EXISTS Users;");
        raw.sqlite3_exec(_db1, "CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
        raw.sqlite3_exec(_db1, "BEGIN;");
        raw.sqlite3_prepare_v2(_db1, "INSERT INTO Users VALUES (?, ?, ?);", out var stmt);
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
        raw.sqlite3_exec(_db1, "COMMIT;");
    }

    [GlobalCleanup(Target = nameof(ReadSpan_SQLitePCL))]
    public void Cleanup_SQLitePCL() => raw.sqlite3_close_v2(_db1);

    [Benchmark(Baseline = true)] // Imposta SQLitePCLRaw come punto di riferimento
    public unsafe void ReadSpan_SQLitePCL()
    {
        raw.sqlite3_prepare_v2(_db1, "SELECT Id, Name, Score FROM Users;", out var stmtRaw);
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



    [GlobalSetup(Target = nameof(ReadSpan_Interop))]
    public void Setup_Interop()
    {
        _db2 = Sqlite3.Open(DbFile);
        _db2.Execute("PRAGMA synchronous = OFF;");
        _db2.Execute("DROP TABLE IF EXISTS Users;");
        _db2.Execute("CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
        _db2.Execute("BEGIN;");
        using (var stmt = _db2.Prepare("INSERT INTO Users VALUES (?, ?, ?);"))
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
        _db2.Execute("COMMIT;");
    }

    [GlobalCleanup(Target = nameof(ReadSpan_Interop))]
    public void Cleanup_Interop() => _db2.Dispose();

    [Benchmark]
    public void ReadSpan_Interop()
    {
        using (var stmt = _db2.Prepare("SELECT Id, Name, Score FROM Users;"))
        {
            while (stmt.Step())
            {
                long id = stmt.GetLong(0);
                ReadOnlySpan<byte> nameSpan = stmt.GetText(1);
                double score = stmt.GetDouble(2);
                _consumer.Consume(id);
                _consumer.Consume(nameSpan[0]);
                _consumer.Consume(score);
            }
        }
    }



    // private sqlite3 _db3;
    // [GlobalSetup(Target = nameof(ReadSpan_SQLitePCL_Ugly))]
    // public void Setup_SQLitePCL_Ugly()
    // {
    //     Batteries_V2.Init();
    //     sqlite3 _db3 = ugly.open(DbFile);
    //     _db3.exec("PRAGMA synchronous = OFF;");
    //     _db3.exec("DROP TABLE IF EXISTS Users;");
    //     _db3.exec("CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
    //     _db3.exec("BEGIN;");
    //     using (sqlite3_stmt stmt = _db3.prepare("INSERT INTO Users VALUES (?, ?, ?);"))
    //     {
    //         for (int u = 0; u < RowCount; u++)
    //         {
    //             stmt.reset();
    //             stmt.bind_int64(1, u);
    //             stmt.bind_text(2, TestString);
    //             stmt.bind_double(3, u * 1.1);
    //             stmt.step();
    //         }
    //     }
    //     _db3.exec("COMMIT;");        
    // }
    // [GlobalCleanup(Target = nameof(ReadSpan_SQLitePCL_Ugly))]
    // public void Cleanup_SQLitePCL_Ugly() => ugly.close_v2(_db3);

    // [Benchmark]
    // public unsafe void ReadSpan_SQLitePCL_Ugly()
    // {
    //     using (sqlite3_stmt stmtUgly = _db3.prepare("SELECT Id, Name, Score FROM Users;"))
    //     {
    //         while (stmtUgly.step() == SQLitePCL.raw.SQLITE_ROW)
    //         {
    //             long id = stmtUgly.column_int64(0);
    //             ReadOnlySpan<byte> nameSpan = stmtUgly.column_blob(1);
    //             double score = stmtUgly.column_double(2);
    //             _consumer.Consume(id);
    //             _consumer.Consume(nameSpan[0]);
    //             _consumer.Consume(score);
    //         }
    //     }
    // }
}
