using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using SQLitePCL; // Serve per il Consumer

namespace CiccioSoft.Sqlite.Interop.Benchmark;

public class ReadString
{
    private const int RowCount = ReadDbStuff.RowCount;
    private const string TestString = ReadDbStuff.TestString;
    private const string DbFile = ReadDbStuff.DbFile;

    private sqlite3 _db1;
    private Sqlite3 _db3;
    private Light.Sqlite3 _db4;
    private Com.Sqlite3 _db5;

    // Il Consumer dice a BenchmarkDotNet di consumare il valore per evitare ottimizzazioni aggressive del JIT/AOT
    private readonly Consumer _consumer = new Consumer();

    [GlobalSetup(Target = nameof(ReadString_SQLitePCL))]
    public void Setup_SQLitePCL() => _db1 = ReadDbStuff.Setup_SQLitePCL();

    [GlobalSetup(Target = nameof(ReadString_Interop))]
    public void Setup_Interop() => _db3 = ReadDbStuff.Setup_Interop();

    [GlobalSetup(Target = nameof(ReadString_InteropLight))]
    public void Setup_InteropLight() => _db4 = ReadDbStuff.Setup_InteropLight();

    [GlobalSetup(Target = nameof(ReadString_InteropCom))]
    public void Setup_InteropCom() => _db5 = ReadDbStuff.Setup_InteropCom();

    [GlobalCleanup(Target = nameof(ReadString_SQLitePCL))]
    public void Cleanup_SQLitePCL() => raw.sqlite3_close_v2(_db1);

    [GlobalCleanup(Target = nameof(ReadString_Interop))]
    public void Cleanup_Interop() => _db3.Dispose();

    [GlobalCleanup(Target = nameof(ReadString_InteropLight))]
    public void Cleanup_InteropLight() => _db4.Dispose();

    [GlobalCleanup(Target = nameof(ReadString_InteropCom))]
    public void Cleanup_InteropCom() => _db5.Dispose();

    // ==========================================
    // BENCHMARK DI LETTURA (SELECT)
    // ==========================================

    [Benchmark(Baseline = true)] // Imposta SQLitePCLRaw come punto di riferimento
    public unsafe void ReadString_SQLitePCL()
    {
        raw.sqlite3_prepare_v2(_db1, "SELECT Id, Name, Score FROM Users;", out var stmtRaw);
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
    public void ReadString_Interop()
    {
        using (var stmt = _db3.Prepare("SELECT Id, Name, Score FROM Users;"))
        {
            while (stmt.Step())
            {
                long id = stmt.GetLong(0);
                string name = stmt.GetTextString(1);
                double score = stmt.GetDouble(2);
                _consumer.Consume(id);
                _consumer.Consume(name);
                _consumer.Consume(score);
            }
        }
    }

    [Benchmark]
    public void ReadString_InteropLight()
    {
        using (var stmt = _db4.Prepare("SELECT Id, Name, Score FROM Users;"))
        {
            while (stmt.Step())
            {
                long id = stmt.GetLong(0);
                string name = stmt.GetTextString(1);
                double score = stmt.GetDouble(2);
                _consumer.Consume(id);
                _consumer.Consume(name);
                _consumer.Consume(score);
            }
        }
    }

    [Benchmark]
    public void ReadString_InteropCom()
    {
        _db5.Prepare("SELECT Id, Name, Score FROM Users;", out var stmt);
        using (stmt)
        {
            // Fintanto che ci sono righe (SQLITE_ROW)
            while (stmt.Step() == Com.SqliteResult.Row)
            {
                long id = stmt.GetLong(0);
                string name = stmt.GetTextString(1);
                double score = stmt.GetDouble(2);
                _consumer.Consume(id);
                _consumer.Consume(name);
                _consumer.Consume(score);
            }
        }
    }
}
