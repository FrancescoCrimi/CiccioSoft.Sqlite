// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Data;
using System.Threading.Tasks;
using CiccioSoft.Sqlite.Interop;
using Xunit;

namespace CiccioSoft.Data.Sqlite;

public class SqliteTransactionTest
{
    [Theory, InlineData(false), InlineData(true)]
    public async Task SqliteTransaction_Dispose_does_not_leave_orphaned_transaction(bool async)
    {
        // Issue #25119 equivalent without relying on inheritance/fakes.
        // In CiccioSoft.Data.Sqlite, SqliteTransaction natively executes ROLLBACK 
        // bypassing SqliteCommand. We simulate a Rollback failure by completing 
        // the transaction externally. Dispose() will attempt to call Rollback(),
        // which will throw. We verify that Dispose catches the error and still 
        // clears the connection's active transaction.

        using var connection = new SqliteConnection("Data Source=:memory:");
        if (async)
            await connection.OpenAsync();
        else
            connection.Open();

        using var transaction = async ? await connection.BeginTransactionAsync() : connection.BeginTransaction();

        // Simulate external rollback (e.g. by SQLite engine itself, or manually)
        // This causes the native SQLite connection to switch to AutoCommit mode.
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "ROLLBACK;";
            if (async)
                await cmd.ExecuteNonQueryAsync();
            else
                cmd.ExecuteNonQuery();
        }

        // At this point, the ADO.NET transaction object doesn't know it was rolled back yet.
        // When we dispose it, it will try to call Rollback().
        // Rollback() will check EnsureActive() -> sees IsAutoCommit() == true -> throws.
        // Dispose() must catch this exception and STILL clear the connection.Transaction.
        if (async)
            await transaction.DisposeAsync();
        else
            transaction.Dispose();

        // Assert that the orphaned transaction is cleared!
        Assert.Null(connection.Transaction);

        // Verify the connection is fully usable and we can start a new transaction
        using var transaction2 = async ? await connection.BeginTransactionAsync() : connection.BeginTransaction();
        Assert.NotNull(connection.Transaction);

        if (async)
            await transaction2.DisposeAsync();
        else
            transaction2.Dispose();

        Assert.Null(connection.Transaction);
    }



    [Fact]
    public void ReadUncommitted_allows_dirty_reads_as_per_sqlite_design()
    {
        // Verifica il comportamento standard di SQLite in Shared Cache:
        // La lettura sporca DEVE essere permessa senza errori.
        const string cs = "Data Source=read-uncommitted-test;Mode=Memory;Cache=Shared";
        using var c1 = new SqliteConnection(cs);
        using var c2 = new SqliteConnection(cs);
        c1.Open(); c2.Open();

        c1.ExecuteNonQuery("CREATE TABLE Data (Value); INSERT INTO Data VALUES (0);");

        using (c1.BeginTransaction())
        {
            c1.ExecuteNonQuery("UPDATE Data SET Value = 1;");

            using (c2.BeginTransaction(IsolationLevel.ReadUncommitted))
            {
                var value = c2.ExecuteScalar<long>("SELECT * FROM Data;");
                // Certifichiamo che SQLite permette la dirty read
                Assert.Equal(1, value);
            }
        }
    }

    [Fact]
    public void ReadUncommitted_respects_exclusive_locks()
    {
        // Questo test certifica che il tuo provider gestisce correttamente 
        // l'eccezione SQLITE_LOCKED quando il conflitto è REALE e FORZATO.
        const string cs = "Data Source=lock-test;Mode=Memory;Cache=Shared";
        using var c1 = new SqliteConnection(cs);
        using var c2 = new SqliteConnection(cs);
        c1.Open(); c2.Open();

        c1.ExecuteNonQuery("CREATE TABLE Data (Value); INSERT INTO Data VALUES (0);");

        // FORZIAMO il conflitto in modo deterministico
        c1.ExecuteNonQuery("BEGIN EXCLUSIVE;");

        c2.DefaultTimeout = 1; // Timeout brevissimo per forzare l'eccezione

        // Certifichiamo che il provider traduce correttamente SQLITE_BUSY 
        // in un'eccezione managed quando il lock è esclusivo.
        var ex = Assert.Throws<SqliteException>(() => c2.ExecuteScalar<long>("SELECT * FROM Data;"));

        // Assert.Equal(SQLITE_LOCKED, ex.SqliteErrorCode);
        Assert.Equal(SqliteResult.Locked, ex.SqliteErrorCode);
    }

}
