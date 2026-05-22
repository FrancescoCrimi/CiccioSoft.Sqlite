using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using AdoNet.Specification.Tests;
using CiccioSoft.Data.Sqlite;

namespace Conformance.Tests;

public class SelectValueFixture : DbFactoryFixture, ISelectValueFixture, IDeleteFixture, IDisposable
{
    public SelectValueFixture() => SqliteDatabase.CreateSelectValueTable(this);
	public void Dispose() => SqliteDatabase.DropSelectValueTable(this);
	public string CreateSelectSql(DbType dbType, ValueKind kind) => SqliteDatabase.CreateSelectSql(dbType, kind);
	public string CreateSelectSql(byte[] value) => SqliteDatabase.CreateSelectSql(value);
	public string SelectNoRows => SqliteDatabase.SelectNoRows;
	public IReadOnlyCollection<DbType> SupportedDbTypes => SqliteDatabase.SupportedDbTypes;
	public Type NullValueExceptionType => SqliteDatabase.NullValueExceptionType;	
	public string DeleteNoRows => SqliteDatabase.DeleteNoRows;
}
