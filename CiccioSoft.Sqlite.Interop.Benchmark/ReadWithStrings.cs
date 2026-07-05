using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Engines;
using SQLitePCL; // Serve per il Consumer

namespace CiccioSoft.Sqlite.Interop.Benchmark;

[Config(typeof(AotConfig))]
[MemoryDiagnoser]
public class ReadWithStrings
{
    private class AotConfig : ManualConfig
    {
        public AotConfig()
        {
            Add(DefaultConfig.Instance);
            // AddJob(Job.Default.WithRuntime(NativeAotRuntime.Net100));
            WithBuildTimeout(TimeSpan.FromMinutes(15)); // Manteniamo il timeout alto per il tuo Pentium
        }
    }

    private const int RowCount = 1_000_000;
    private const string TestString = "User_Performance_Test_String_12345";

    private sqlite3 _db0;
    private Sqlite3 _db1;
    private Light.Sqlite3 _db2;
    private Com.Sqlite3 _db3;

    // Il Consumer dice a BenchmarkDotNet di consumare il valore per evitare ottimizzazioni aggressive del JIT/AOT
    private readonly Consumer _consumer = new Consumer();

    [GlobalSetup]
    public void Setup()
    {
        // Inizializzazione necessaria per SQLitePCLRaw
        Batteries_V2.Init();

        // Prepariamo il DB in memoria una volta sola prima del test

        raw.sqlite3_open(":memory:", out _db0);
        raw.sqlite3_exec(_db0, "PRAGMA synchronous = OFF;");
        raw.sqlite3_exec(_db0, "CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
        raw.sqlite3_exec(_db0, "BEGIN;");
        raw.sqlite3_prepare_v2(_db0, "INSERT INTO Users VALUES (?, ?, ?);", out var stmtRaw);
        for (int i = 0; i < RowCount; i++)
        {
            raw.sqlite3_reset(stmtRaw);
            raw.sqlite3_bind_int64(stmtRaw, 1, i);
            raw.sqlite3_bind_text(stmtRaw, 2, TestString);
            raw.sqlite3_bind_double(stmtRaw, 3, i * 1.1);
            raw.sqlite3_step(stmtRaw);
        }
        raw.sqlite3_exec(_db0, "COMMIT;");
        raw.sqlite3_finalize(stmtRaw);


        _db1 = Sqlite3.Open(":memory:");
        _db1.Execute("PRAGMA synchronous = OFF;");
        _db1.Execute("CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
        _db1.Execute("BEGIN;");
        using (var stmt = _db1.Prepare("INSERT INTO Users VALUES (?, ?, ?);"))
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
        _db1.Execute("COMMIT;");


        Light.Sqlite3.Open(":memory:", out _db2);
        _db2.Execute("PRAGMA synchronous = OFF;");
        _db2.Execute("CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
        _db2.Execute("BEGIN;");
        _db2.Prepare("INSERT INTO Users VALUES (?, ?, ?);", out var stmt2);
        using (stmt2)
        {
            for (int i = 0; i < RowCount; i++)
            {
                stmt2?.Reset();
                stmt2?.BindLong(1, i);
                stmt2?.BindText(2, TestString);
                stmt2?.BindDouble(3, i * 1.1);
                stmt2?.Step();
            }
        }
        _db2.Execute("COMMIT;");


        Com.Sqlite3.Open(":memory:", out _db3);
        _db3.Execute("PRAGMA synchronous = OFF;");
        _db3.Execute("CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
        _db3.Execute("BEGIN;");
        _db3.Prepare("INSERT INTO Users VALUES (?, ?, ?);", out var stmt3);
        using (stmt3)
        {
            for (int i = 0; i < RowCount; i++)
            {
                stmt3?.Reset();
                stmt3?.BindLong(1, i);
                stmt3?.BindText(2, TestString);
                stmt3?.BindDouble(3, i * 1.1);
                stmt3?.Step();
            }
        }
        _db3.Execute("COMMIT;");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        raw.sqlite3_close(_db0);
        _db1?.Dispose();
        _db2?.Dispose();
        _db3?.Dispose();
    }

    // ==========================================
    // BENCHMARK DI LETTURA (SELECT)
    // ==========================================

    [Benchmark(Baseline = true)] // Imposta SQLitePCLRaw come punto di riferimento
    public unsafe void ReadZeroAllocation_SQLitePCL()
    {
        raw.sqlite3_prepare_v2(_db0, "SELECT Id, Name, Score FROM Users;", out var stmtRaw);
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

    [Benchmark]
    public void ReadWithStrings_Interop()
    {
        using (var stmt = _db1.Prepare("SELECT Id, Name, Score FROM Users;"))
        {
            while (stmt.Step())
            {
                long id = stmt.GetLong(0);
                string name = stmt.GetTextString(1)!;
                double score = stmt.GetDouble(2);
                _consumer.Consume(id);
                _consumer.Consume(name);
                _consumer.Consume(score);
            }
        }
    }

    [Benchmark]
    public void ReadWithStrings_InteropLight()
    {
        _db2.Prepare("SELECT Id, Name, Score FROM Users;", out var stmt);
        using (stmt)
        {
            while (stmt?.Step() == Light.SqliteResult.Row)
            {
                long id = stmt.GetLong(0);
                string name = stmt.GetTextString(1)!;
                double score = stmt.GetDouble(2);
                _consumer.Consume(id);
                _consumer.Consume(name);
                _consumer.Consume(score);
            }
        }
    }

    [Benchmark]
    public void ReadWithStrings_InteropCom()
    {
        _db3.Prepare("SELECT Id, Name, Score FROM Users;", out var stmt);
        using (stmt)
        {
            // Fintanto che ci sono righe (SQLITE_ROW)
            while (stmt?.Step() == Com.SqliteResult.Row)
            {
                long id = stmt.GetLong(0);
                string name = stmt.GetTextString(1)!;
                double score = stmt.GetDouble(2);
                _consumer.Consume(id);
                _consumer.Consume(name);
                _consumer.Consume(score);
            }
        }
    }

}
