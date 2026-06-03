using System;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Xunit;

namespace CiccioSoft.Data.Sqlite;

public class SqliteParameterCollectionContractTests
{
    [Fact(Skip = "not supported")]
    public void Add_rejects_invalid_edge_case_names()
    {
        var command = new SqliteCommand();
        DbParameterCollection parameters = command.Parameters;

        Assert.Throws<ArgumentException>(() => parameters.Add(new SqliteParameter { ParameterName = "   ", Value = 1 }));
        Assert.Throws<ArgumentException>(() => parameters.Add(new SqliteParameter { ParameterName = "@", Value = 1 }));
    }    
}
