using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Order;
using SQLitePCL;

namespace CiccioSoft.Sqlite.Native.Benchmark;

[MemoryDiagnoser] // Certifica l'impatto della memoria e del GC
[Orderer(SummaryOrderPolicy.FastestToSlowest)] // Ordina dal più veloce al più lento
[RankColumn] // Aggiunge una colonna con la classifica (1°, 2°, ecc.)
public class SqliteBenchmark
{
    private string _dbPCLRaw = Path.Combine(Path.GetTempPath(), "benchmark_pclraw.db");
    private string _dbCiccioSoft = Path.Combine(Path.GetTempPath(), "benchmark_ciccioSoft.db");

    // Definiamo i due scaglioni di record richiesti dai tuoi test
    // [Params(100000, 1000000)]
    [Params(10000)]
    public int N;

    // Il Consumer dice a BenchmarkDotNet di consumare il valore per evitare ottimizzazioni aggressive del JIT/AOT
    private readonly Consumer _consumer = new Consumer();

    [GlobalSetup]
    public void GlobalSetup()
    {
        if (File.Exists(_dbPCLRaw)) File.Delete(_dbPCLRaw);
        if (File.Exists(_dbCiccioSoft)) File.Delete(_dbCiccioSoft);

        SQLitePCL.Batteries_V2.Init();
        NativeLibraryResolver.Configure(NativeSource.SourceGear);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        if (File.Exists(_dbPCLRaw)) File.Delete(_dbPCLRaw);
        if (File.Exists(_dbCiccioSoft)) File.Delete(_dbCiccioSoft);
    }

    [IterationSetup(Target = nameof(PCLRaw_ReadWrite))]
    public void IterationSetup_PCLRaw()
    {
        var rc = raw.sqlite3_open(_dbPCLRaw, out var db);
        raw.sqlite3_exec(db, "DROP TABLE IF EXISTS Users;");
        raw.sqlite3_exec(db, "CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
        raw.sqlite3_close(db);
    }

    [IterationSetup(Target = nameof(CiccioSoft_ReadWrite))]
    public void IterationSetup_CiccioSoft()
    {
        using (var connection = Connection.Open(_dbCiccioSoft, OpenFlags.ReadWrite | OpenFlags.Create))
        {
            connection.Execute("DROP TABLE IF EXISTS Users;");
            connection.Execute("CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
        }
    }

    [Benchmark(Baseline = true)] // Imposta Microsoft come termine di paragone (1.00)
    public void PCLRaw_ReadWrite()
    {
        var rc = raw.sqlite3_open(_dbPCLRaw, out var db);

        // Avvio transazione per SQLitePCLRaw
        raw.sqlite3_exec(db, "BEGIN TRANSACTION;");

        // Esempio Scrittura massiva standard
        raw.sqlite3_prepare_v2(db, "INSERT INTO Users (Id, Name, Score) VALUES (?, ?, ?);", out var stmtInsert);
        for (int i = 0; i < N; i++)
        {
            raw.sqlite3_bind_int(stmtInsert, 1, i);
            raw.sqlite3_bind_text(stmtInsert, 2, "TestStringaBreve");
            raw.sqlite3_bind_double(stmtInsert, 3, i * 1.1);
            raw.sqlite3_step(stmtInsert);
            raw.sqlite3_reset(stmtInsert);
        }
        raw.sqlite3_finalize(stmtInsert);

        // Commit transazione
        raw.sqlite3_exec(db, "COMMIT;");

        // Lettura massiva
        raw.sqlite3_prepare_v2(db, "SELECT Id, Name, Score FROM Users;", out var stmtSelect);
        while (raw.sqlite3_step(stmtSelect) == raw.SQLITE_ROW)
        {
            int id = raw.sqlite3_column_int(stmtSelect, 0);
            string name = raw.sqlite3_column_text(stmtSelect, 1).utf8_to_string();
            double score = raw.sqlite3_column_double(stmtSelect, 2);
            _consumer.Consume(id);
            _consumer.Consume(name);
            _consumer.Consume(score);
        }
        raw.sqlite3_finalize(stmtSelect);

        raw.sqlite3_close(db);
    }

    [Benchmark]
    public void CiccioSoft_ReadWrite()
    {
        using (var connection = Connection.Open(_dbCiccioSoft, OpenFlags.ReadWrite | OpenFlags.Create))
        {
            connection.Execute("BEGIN TRANSACTION;");
            using (var stmt = connection.Prepare("INSERT INTO Users VALUES (?, ?, ?);"))
            {
                for (int i = 0; i < N; i++)
                {
                    stmt.Reset();
                    stmt.BindLong(1, i);
                    stmt.BindText(2, "TestStringaBreve");
                    stmt.BindDouble(3, i * 1.1);
                    stmt.Step();
                }
            }
            connection.Execute("COMMIT;");

            using (var stmt = connection.Prepare("SELECT Id, Name, Score FROM Users;"))
            {
                while (stmt.Step())
                {
                    long id = stmt.GetLong(0);
                    string name = stmt.GetText(1)!;
                    double score = stmt.GetDouble(2);
                    _consumer.Consume(id);
                    _consumer.Consume(name);
                    _consumer.Consume(score);
                }
            }
        }
    }
}
