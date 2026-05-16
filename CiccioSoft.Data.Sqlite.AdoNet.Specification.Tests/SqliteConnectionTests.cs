using AdoNet.Specification.Tests;
using CiccioSoft.Data.Sqlite; // Il tuo namespace

namespace CiccioSoft.Data.Sqlite.AdoNet.Specification.Tests;

public class SqliteConnectionTests : ConnectionTestBase<SqliteDbFactoryFixture>
{
    public SqliteConnectionTests(SqliteDbFactoryFixture fixture)
        : base(fixture)
    {
    }
}
