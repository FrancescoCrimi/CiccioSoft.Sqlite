// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Data.Common;

namespace CiccioSoft.Data.Sqlite;

/// <summary>
/// An implementation of <see cref="DbProviderFactory"/> that creates CiccioSoft.Data.Sqlite objects.
/// </summary>
public sealed class SqliteFactory : DbProviderFactory
{
	/// <summary>
	/// Provides an instance of <see cref="DbProviderFactory"/> that can create CiccioSoft.Data.Sqlite objects.
	/// </summary>
	public static readonly SqliteFactory Instance = new();

	/// <summary>
	/// Creates a new <see cref="SqliteCommand"/> object.
	/// </summary>
	public override DbCommand CreateCommand() => new SqliteCommand();

	/// <summary>
	/// Creates a new <see cref="SqliteConnection"/> object.
	/// </summary>
	public override DbConnection CreateConnection() => new SqliteConnection();

	/// <summary>
	/// Creates a new <see cref="SqliteConnectionStringBuilder"/> object.
	/// </summary>
	public override DbConnectionStringBuilder CreateConnectionStringBuilder() => new SqliteConnectionStringBuilder();

	/// <summary>
	/// Creates a new <see cref="SqliteParameter"/> object.
	/// </summary>
	public override DbParameter CreateParameter() => new SqliteParameter();

	/// <summary>
	/// Returns <c>false</c>.
	/// </summary>
	/// <remarks><see cref="DbDataSourceEnumerator"/> is not supported by CiccioSoft.Data.Sqlite.</remarks>
	public override bool CanCreateDataSourceEnumerator => false;

	public override bool CanCreateCommandBuilder => false;

	public override bool CanCreateDataAdapter => false;

	public override bool CanCreateBatch => false;
}
