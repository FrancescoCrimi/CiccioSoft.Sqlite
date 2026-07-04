using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using SQLitePCL;
using CiccioSoft.Sqlite.Interop;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace CiccioSoft.Sqlite.Interop.Benchmark;

// Configura il benchmark per calcolare anche la memoria allocata
[Config(typeof(AotConfig))]
[MemoryDiagnoser]
public class DotNetBenchmarkWriter
{

    // La tua classe di configurazione personalizzata per l'AOT
    private class AotConfig : ManualConfig
    {
        public AotConfig()
        {
            // 1. Usa la configurazione di base predefinita
            Add(DefaultConfig.Instance);

            // Dice a BenchmarkDotNet di usare il compilatore Ilc nativo di .NET 10
            // AddJob(Job.Default.WithRuntime(NativeAotRuntime.Net10_0)); 

            // 3. ESTENDI IL TIMEOUT DELLA COMPILAZIONE A 15 MINUTI
            // Questo darà al tuo Pentium il tempo di applicare le ottimizzazioni AOT
            WithBuildTimeout(TimeSpan.FromMinutes(30));
        }
    }
    
    private const string DbFile1 = @"C:\Users\franc\Dev\CiccioSoft.Sqlite\CiccioSoft.Sqlite.Interop.Benchmark\rawpcl_test.db";
    private const string DbFile2 = @"C:\Users\franc\Dev\CiccioSoft.Sqlite\CiccioSoft.Sqlite.Interop.Benchmark\wrapper_test.db";
    private const string DbFile3 = @"C:\Users\franc\Dev\CiccioSoft.Sqlite\CiccioSoft.Sqlite.Interop.Benchmark\newwrap_test.db";
    private const string DbFile4 = @"C:\Users\franc\Dev\CiccioSoft.Sqlite\CiccioSoft.Sqlite.Interop.Benchmark\cazzo_test.db";

    private const int RowCount = 100_000; // Ridotto a 100k perché BenchmarkDotNet esegue i test molte volte
    private const string TestString = "User_Performance_Test_String_12345";

    private sqlite3 _dbRaw;
    private sqlite3_stmt _stmtRaw;

    // Eseguito prima di ogni iterazione per pulire l'ambiente
    [GlobalSetup]
    public void Setup()
    {
        Batteries_V2.Init();

        // Pulizia file precedenti
        // if (File.Exists(DbFile1)) File.Delete(DbFile1);
        // if (File.Exists(DbFile2)) File.Delete(DbFile2);

        // Prepariamo i database per i test di lettura
        // PrepareDatabaseForRead(DbFile1);
        // PrepareDatabaseForRead(DbFile2);
    }

    private void PrepareDatabaseForRead(string fileName)
    {
        // Logica minima per creare il DB con 100k righe prima del test di lettura

        //         string sql = """
        //         CREATE TABLE products (
        //             id    INTEGER PRIMARY KEY AUTOINCREMENT,
        //             name  TEXT    NOT NULL,
        //             price REAL    NOT NULL,
        //             stock INTEGER NOT NULL DEFAULT 0
        //         );
        //         """;
    }

    // ==========================================
    // BENCHMARK DI SCRITTURA (IN-MEMORY per eliminare l'I/O del disco)
    // ==========================================

    [Benchmark(Baseline = true)] // Imposta SQLitePCLRaw come punto di riferimento
    public void Write_SQLitePCLRaw()
    {
        sqlite3 dbRaw;
        raw.sqlite3_open(":memory:", out dbRaw); // Usiamo :memory: per non subire l'I/O del disco
        // raw.sqlite3_exec(dbRaw, "PRAGMA journal_mode = WAL;");
        raw.sqlite3_exec(dbRaw, "PRAGMA synchronous = OFF;");
        raw.sqlite3_exec(dbRaw, "DROP TABLE IF EXISTS Users;");
        raw.sqlite3_exec(dbRaw, "CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
        raw.sqlite3_exec(dbRaw, "BEGIN;");

        sqlite3_stmt stmtRaw;
        raw.sqlite3_prepare_v2(dbRaw, "INSERT INTO Users VALUES (?, ?, ?);", out stmtRaw);
        for (int i = 0; i < RowCount; i++)
        {
            raw.sqlite3_reset(stmtRaw);
            raw.sqlite3_bind_int64(stmtRaw, 1, i);
            raw.sqlite3_bind_text(stmtRaw, 2, TestString);
            raw.sqlite3_bind_double(stmtRaw, 3, i * 1.1);
            raw.sqlite3_step(stmtRaw);
        }
        raw.sqlite3_exec(dbRaw, "COMMIT;");
        raw.sqlite3_finalize(stmtRaw);
        raw.sqlite3_close(dbRaw);
    }

    // [Benchmark]
    // public void Write_CiccioSoftInterop()
    // {
    //     using (var db = CiccioSoft.Sqlite.Interop.Sqlite3.Open(":memory:")) // In-Memory
    //     {
    //         // db.Execute("PRAGMA journal_mode = WAL;");
    //         db.Execute("PRAGMA synchronous = OFF;");
    //         db.Execute("DROP TABLE IF EXISTS Users;");
    //         db.Execute("CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
    //         db.Execute("BEGIN;");
    //         using var stmt = db.Prepare("INSERT INTO Users VALUES (?, ?, ?);");
    //         for (int i = 0; i < RowCount; i++)
    //         {
    //             stmt.Reset();
    //             stmt.BindLong(1, i);
    //             stmt.BindText(2, TestString);
    //             stmt.BindDouble(3, i * 1.1);
    //             stmt.Step();
    //         }
    //         db.Execute("COMMIT;");
    //     }
    // }

    // [Benchmark]
    // public void Write_CiccioSoftExp()
    // {
    //     CiccioSoft.Sqlite.Interop.Exp.Sqlite3 db;
    //     CiccioSoft.Sqlite.Interop.Exp.Sqlite3.Open(":memory:", out db);  // In-Memory
    //     using (db)
    //     {
    //         // db.Execute("PRAGMA journal_mode = WAL;");
    //         db.Execute("PRAGMA synchronous = OFF;");
    //         db.Execute("DROP TABLE IF EXISTS Users;");
    //         db.Execute("CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
    //         db.Execute("BEGIN;");
    //         CiccioSoft.Sqlite.Interop.Exp.Sqlite3Stmt stmt;
    //         db.Prepare("INSERT INTO Users VALUES (?, ?, ?);", out stmt);
    //         using (stmt)
    //         {
    //             for (int i = 0; i < RowCount; i++)
    //             {
    //                 stmt.Reset();
    //                 stmt.BindLong(1, i);
    //                 stmt.BindText(2, TestString);
    //                 stmt.BindDouble(3, i * 1.1);
    //                 stmt.Step();
    //             }
    //             db.Execute("COMMIT;");
    //         }
    //     }
    // }

    [Benchmark]
    public void Write_CiccioSoftFast()
    {
        CiccioSoft.Sqlite.Interop.Fast.Sqlite3 db;
        CiccioSoft.Sqlite.Interop.Fast.Sqlite3.Open(":memory:", out db);  // In-Memory
        using (db)
        {
            // db.Execute("PRAGMA journal_mode = WAL;");
            db.Execute("PRAGMA synchronous = OFF;");
            db.Execute("DROP TABLE IF EXISTS Users;");
            db.Execute("CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
            db.Execute("BEGIN;");
            CiccioSoft.Sqlite.Interop.Fast.Sqlite3Stmt stmt;
            db.Prepare("INSERT INTO Users VALUES (?, ?, ?);", out stmt);
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
                db.Execute("COMMIT;");
            }
        }
    }
}
