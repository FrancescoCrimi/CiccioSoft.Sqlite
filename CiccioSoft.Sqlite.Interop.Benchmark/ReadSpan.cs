using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using SQLitePCL;
using SQLitePCL.Ugly;

namespace CiccioSoft.Sqlite.Interop.Benchmark;

public class ReadSpan
{
    private const int RowCount = ReadDbStuff.RowCount;
    private const string TestString = ReadDbStuff.TestString;
    private const string DbFile = ReadDbStuff.DbFile;

    private sqlite3 _db1;
    // private sqlite3 _db2;
    private Sqlite3 _db3;
    private Light.Sqlite3 _db4;
    // private Com.Sqlite3 _db5;

    // Il Consumer dice a BenchmarkDotNet di consumare il valore per evitare ottimizzazioni aggressive del JIT/AOT
    private readonly Consumer _consumer = new Consumer();

    [GlobalSetup(Target = nameof(ReadSpan_SQLitePCL))]
    public void Setup_SQLitePCL() => _db1 = ReadDbStuff.Setup_SQLitePCL();

    // [GlobalSetup(Target = nameof(ReadSpan_SQLitePCL_Ugly))]
    // public void Setup_SQLitePCL_Ugly() => _db2 = ReadDbStuff.Setup_SQLitePCL_Ugly();

    [GlobalSetup(Target = nameof(ReadSpan_Interop))]
    public void Setup_Interop() => _db3 = ReadDbStuff.Setup_Interop();

    [GlobalSetup(Target = nameof(ReadSpan_InteropLight))]
    public void Setup_InteropLight() => _db4 = ReadDbStuff.Setup_InteropLight();

    // [GlobalSetup(Target = nameof(ReadSpan_InteropCom))]
    // public void Setup_InteropCom() => _db5 = ReadDbStuff.Setup_InteropCom();

    [GlobalCleanup(Target = nameof(ReadSpan_SQLitePCL))]
    public void Cleanup_SQLitePCL() => raw.sqlite3_close_v2(_db1);

    // [GlobalCleanup(Target = nameof(ReadSpan_SQLitePCL_Ugly))]
    // public void Cleanup_SQLitePCL_Ugly() => ugly.close_v2(_db2);

    [GlobalCleanup(Target = nameof(ReadSpan_Interop))]
    public void Cleanup_Interop() => _db3.Dispose();

    [GlobalCleanup(Target = nameof(ReadSpan_InteropLight))]
    public void Cleanup_InteropLight() => _db4.Dispose();

    // [GlobalCleanup(Target = nameof(ReadSpan_InteropCom))]
    // public void Cleanup_InteropCom() => _db5.Dispose();

    // ==========================================
    // BENCHMARK DI LETTURA (SELECT)
    // ==========================================

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

    // [Benchmark]
    // public unsafe void ReadSpan_SQLitePCL_Ugly()
    // {
    //     using (sqlite3_stmt stmtUgly = _db2.prepare("SELECT Id, Name, Score FROM Users;"))
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

    [Benchmark]
    public void ReadSpan_Interop()
    {
        using (var stmt = _db3.Prepare("SELECT Id, Name, Score FROM Users;"))
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

    [Benchmark]
    public void ReadSpan_InteropLight()
    {
        using (var stmt = _db4.Prepare("SELECT Id, Name, Score FROM Users;"))
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

    // [Benchmark]
    // public void ReadSpan_InteropCom()
    // {
    //     _db5.Prepare("SELECT Id, Name, Score FROM Users;", out var stmt);
    //     using (stmt)
    //     {
    //         while (stmt.Step() == Com.SqliteResult.Row)
    //         {
    //             long id = stmt.GetLong(0);
    //             ReadOnlySpan<byte> nameSpan = stmt.GetText(1);
    //             double score = stmt.GetDouble(2);
    //             _consumer.Consume(id);
    //             _consumer.Consume(nameSpan[0]);
    //             _consumer.Consume(score);
    //         }
    //     }
    // }
}
