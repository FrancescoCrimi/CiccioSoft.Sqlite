using SQLitePCL;
using SQLitePCL.Ugly;

namespace CiccioSoft.Sqlite.Interop.Benchmark;

public static class ReadDbStuff
{
    public const int RowCount = 100_000;
    public const string TestString = "User_Performance_Test_String_12345";
    public const string DbFile = @"C:\Users\franc\Dev\CiccioSoft.Sqlite\CiccioSoft.Sqlite.Interop.Benchmark\read.db";
    // public const string DbFile = ":memory:";


    public static sqlite3 Setup_SQLitePCL()
    {
        Batteries_V2.Init();
        raw.sqlite3_open(DbFile, out sqlite3 db);
        raw.sqlite3_exec(db, "PRAGMA synchronous = OFF;");
        raw.sqlite3_exec(db, "DROP TABLE IF EXISTS Users;");
        raw.sqlite3_exec(db, "CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
        raw.sqlite3_exec(db, "BEGIN;");
        raw.sqlite3_prepare_v2(db, "INSERT INTO Users VALUES (?, ?, ?);", out var stmt);
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
        raw.sqlite3_exec(db, "COMMIT;");
        return db;
    }

    public static sqlite3 Setup_SQLitePCL_Ugly()
    {
        Batteries_V2.Init();
        sqlite3 db = ugly.open(DbFile);
        db.exec("PRAGMA synchronous = OFF;");
        db.exec("DROP TABLE IF EXISTS Users;");
        db.exec("CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
        db.exec("BEGIN;");
        using (sqlite3_stmt stmt = db.prepare("INSERT INTO Users VALUES (?, ?, ?);"))
        {
            for (int u = 0; u < RowCount; u++)
            {
                stmt.reset();
                stmt.bind_int64(1, u);
                stmt.bind_text(2, TestString);
                stmt.bind_double(3, u * 1.1);
                stmt.step();
            }
        }
        db.exec("COMMIT;");
        return db;
    }

    public static Sqlite3 Setup_Interop()
    {
        Sqlite3 db = Sqlite3.Open(DbFile);
        db.Execute("PRAGMA synchronous = OFF;");
        db.Execute("DROP TABLE IF EXISTS Users;");
        db.Execute("CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
        db.Execute("BEGIN;");
        using (var stmt = db.Prepare("INSERT INTO Users VALUES (?, ?, ?);"))
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
        db.Execute("COMMIT;");
        return db;
    }

    public static Light.Sqlite3 Setup_InteropLight()
    {
        Light.Sqlite3 db =Light.Sqlite3.Open(DbFile);
        db.Execute("PRAGMA synchronous = OFF;");
        db.Execute("DROP TABLE IF EXISTS Users;");
        db.Execute("CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
        db.Execute("BEGIN;");
        Light.Sqlite3Stmt stmt = db.Prepare("INSERT INTO Users VALUES (?, ?, ?);");
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
        }
        db.Execute("COMMIT;");
        return db;
    }

    // public static Com.Sqlite3 Setup_InteropCom()
    // {
    //     Com.Sqlite3.Open(DbFile, out Com.Sqlite3 db);
    //     db.Execute("PRAGMA synchronous = OFF;");
    //     db.Execute("DROP TABLE IF EXISTS Users;");
    //     db.Execute("CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
    //     db.Execute("BEGIN;");
    //     db.Prepare("INSERT INTO Users VALUES (?, ?, ?);", out var stmt);
    //     using (stmt)
    //     {
    //         for (int i = 0; i < RowCount; i++)
    //         {
    //             stmt.Reset();
    //             stmt.BindLong(1, i);
    //             stmt.BindText(2, TestString);
    //             stmt.BindDouble(3, i * 1.1);
    //             stmt.Step();
    //         }
    //     }
    //     db.Execute("COMMIT;");
    //     return db;
    // }

    // Pulizia file precedenti
    // if (File.Exists(DbFile1)) File.Delete(DbFile1);
    // if (File.Exists(DbFile2)) File.Delete(DbFile2);

    //         string sql = """
    //         CREATE TABLE products (
    //             id    INTEGER PRIMARY KEY AUTOINCREMENT,
    //             name  TEXT    NOT NULL,
    //             price REAL    NOT NULL,
    //             stock INTEGER NOT NULL DEFAULT 0
    //         );
    //         """;


    // public static void Cleanup_SQLitePCL()
    // {
    //     raw.sqlite3_close_v2(_db1);
    // }

    // public static void Cleanup_SQLitePCL_Ugly()
    // {
    //     ugly.close_v2(_db2);
    // }

    // public  static void Cleanup_Interop()
    // {
    //     _db3.Dispose();
    // }

    // public static void Cleanup_InteropLight()
    // {
    //     _db4.Dispose();
    // }

    // public static void Cleanup_InteropCom()
    // {
    //     _db5.Dispose();
    // }
}