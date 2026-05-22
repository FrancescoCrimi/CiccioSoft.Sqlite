using System;
using System.Data.Common;
using AdoNet.Specification.Tests;
// using CiccioSoft.Data.Sqlite;
using Microsoft.Data.Sqlite;

namespace Conformance.Tests;

public class DbFactoryFixture : IDbFactoryFixture
{
	public DbProviderFactory Factory => SqliteFactory.Instance;
	public string ConnectionString => "data source=temp.db";
}
