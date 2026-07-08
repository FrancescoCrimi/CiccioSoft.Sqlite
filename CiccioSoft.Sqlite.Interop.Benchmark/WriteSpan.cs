using System;
using BenchmarkDotNet.Attributes;
using SQLitePCL;

namespace CiccioSoft.Sqlite.Interop.Benchmark;

public class WriteSpan
{
    private const string DbFile = @"C:\Users\franc\Dev\CiccioSoft.Sqlite\CiccioSoft.Sqlite.Interop.Benchmark\write.db";
    private const int RowCount = 1_000_000; // Ridotto a 100k perché BenchmarkDotNet esegue i test molte volte
    // private const string TestString = "User_Performance_Test_String_12345";
    private static ReadOnlySpan<byte> TestString => "User_Performance_Test_String_12345"u8;

    private sqlite3 _db1;
    private Sqlite3 _db3;
    private Light.Sqlite3 _db4;
    private Com.Sqlite3 _db5;

    [GlobalSetup(Target = nameof(WriteSpan_SQLitePCL))]
    public void GlobalSetup_SQLitePCL()
    {
        Batteries_V2.Init();
        raw.sqlite3_open(DbFile, out _db1); // Usiamo :memory: per non subire l'I/O del disco
        raw.sqlite3_exec(_db1, "PRAGMA journal_mode = WAL;");
        raw.sqlite3_exec(_db1, "PRAGMA synchronous = OFF;");
    }

    [GlobalCleanup(Target = nameof(WriteSpan_SQLitePCL))]
    public void GlobalCleanup_SQLitePCL() => raw.sqlite3_close(_db1);

    [IterationSetup(Target = nameof(WriteSpan_SQLitePCL))]
    public void IterationSetup_SQLitePCL()
    {
        raw.sqlite3_exec(_db1, "DROP TABLE IF EXISTS Users;");
        raw.sqlite3_exec(_db1, "CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
    }

    [Benchmark(Baseline = true)] // Imposta SQLitePCLRaw come punto di riferimento
    public void WriteSpan_SQLitePCL()
    {
        raw.sqlite3_exec(_db1, "BEGIN;");
        raw.sqlite3_prepare_v2(_db1, "INSERT INTO Users VALUES (?, ?, ?);", out sqlite3_stmt stmtRaw);
        using (stmtRaw)
        {
            for (int i = 0; i < RowCount; i++)
            {
                raw.sqlite3_reset(stmtRaw);
                raw.sqlite3_bind_int64(stmtRaw, 1, i);
                raw.sqlite3_bind_text(stmtRaw, 2, TestString);
                raw.sqlite3_bind_double(stmtRaw, 3, i * 1.1);
                raw.sqlite3_step(stmtRaw);
            }
        }
        raw.sqlite3_exec(_db1, "COMMIT;");
    }



    [GlobalSetup(Target = nameof(WriteSpan_Interop))]
    public void GlobalSetup_Interop()
    {
        _db3 = Sqlite3.Open(DbFile);
        _db3.Execute("PRAGMA journal_mode = WAL;");
        _db3.Execute("PRAGMA synchronous = OFF;");
    }

    [GlobalCleanup(Target = nameof(WriteSpan_Interop))]
    public void GlobalCleanup_Interop() => _db3.Dispose();

    [IterationSetup(Target = nameof(WriteSpan_Interop))]
    public void IterationSetup_Interop()
    {
        _db3.Execute("DROP TABLE IF EXISTS Users;");
        _db3.Execute("CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
    }

    [Benchmark]
    public void WriteSpan_Interop()
    {
        _db3.Execute("BEGIN;");
        using (var stmt = _db3.Prepare("INSERT INTO Users VALUES (?, ?, ?);"))
        {
            for (int i = 0; i < RowCount; i++)
            {
                stmt.Reset();
                stmt.BindLong(1, i);
                stmt.BindText(2, TestString);
                stmt.BindDouble(3, i * 1.1);
                stmt.Step();
            }
            _db3.Execute("COMMIT;");
        }
    }



    [GlobalSetup(Target = nameof(WriteSpan_InteropLight))]
    public void GlobalSetup_InteropLight()
    {
        Light.Sqlite3.Open(DbFile, out _db4);
        _db4.Execute("PRAGMA journal_mode = WAL;");
        _db4.Execute("PRAGMA synchronous = OFF;");
    }

    [GlobalCleanup(Target = nameof(WriteSpan_InteropLight))]
    public void GlobalCleanup_InteropLight() => _db4.Dispose();

    [IterationSetup(Target = nameof(WriteSpan_InteropLight))]
    public void IterationSetup_InteropLight()
    {
        _db4.Execute("DROP TABLE IF EXISTS Users;");
        _db4.Execute("CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
    }

    [Benchmark]
    public void WriteSpan_InteropLight()
    {
        _db4.Execute("BEGIN;");
        _db4.Prepare("INSERT INTO Users VALUES (?, ?, ?);", out Light.Sqlite3Stmt stmt);
        using (stmt)
        {
            for (int i = 0; i < RowCount; i++)
            {
                stmt.Reset();
                stmt.BindLong(1, i);
                stmt.BindText(2, TestString);
                stmt.BindDouble(3, i * 1.1);
                stmt.Step();
            }
            _db4.Execute("COMMIT;");
        }
    }



    [GlobalSetup(Target = nameof(WriteSpan_InteropCom))]
    public void GlobalSetup_InteropCom()
    {
        Com.Sqlite3.Open(DbFile, out _db5);
        _db5.Execute("PRAGMA journal_mode = WAL;");
        _db5.Execute("PRAGMA synchronous = OFF;");
    }

    [GlobalCleanup(Target = nameof(WriteSpan_InteropCom))]
    public void GlobalCleanup_InteropCom() => _db5.Dispose();

    [IterationSetup(Target = nameof(WriteSpan_InteropCom))]
    public void IterationSetup_InteropCom()
    {
        _db5.Execute("DROP TABLE IF EXISTS Users;");
        _db5.Execute("CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
    }

    [Benchmark]
    public void WriteSpan_InteropCom()
    {
        _db5.Execute("BEGIN;");
        _db5.Prepare("INSERT INTO Users VALUES (?, ?, ?);", out Com.Sqlite3Stmt stmt);
        using (stmt)
        {
            for (int i = 0; i < RowCount; i++)
            {
                stmt.Reset();
                stmt.BindLong(1, i);
                stmt.BindText(2, TestString);
                stmt.BindDouble(3, i * 1.1);
                stmt.Step();
            }
            _db5.Execute("COMMIT;");
        }
    }
}
