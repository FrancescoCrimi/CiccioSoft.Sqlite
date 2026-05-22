using System.Data.Common;

namespace CiccioSoft.Data.Sqlite;

/// <summary>
/// An implementation of <see cref="DbProviderFactory"/> that creates SqliteConnector objects.
/// </summary>
public sealed class SqliteConnectorFactory : DbProviderFactory
{
	/// <summary>
	/// Provides an instance of <see cref="DbProviderFactory"/> that can create MySqlConnector objects.
	/// </summary>
	public static readonly SqliteConnectorFactory Instance = new();

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
	/// Creates a new <see cref="SqliteCommandBuilder"/> object.
	/// </summary>
	// public override DbCommandBuilder CreateCommandBuilder() => new SqliteCommandBuilder();

	/// <summary>
	/// Creates a new <see cref="SqliteDataAdapter"/> object.
	/// </summary>
	// public override DbDataAdapter CreateDataAdapter() => new SqliteDataAdapter();

	/// <summary>
	/// Returns <c>false</c>.
	/// </summary>
	/// <remarks><see cref="DbDataSourceEnumerator"/> is not supported by MySqlConnector.</remarks>
	public override bool CanCreateDataSourceEnumerator => false;

	/// <summary>
	/// Returns <c>false</c>.
	/// </summary>
	public override bool CanCreateCommandBuilder => false;

	/// <summary>
	/// Returns <c>true</c>.
	/// </summary>
	public override bool CanCreateDataAdapter => false;

#pragma warning disable CA1822 // Mark members as static
	/// <summary>
	/// Creates a new <see cref="MySqlBatch"/> object.
	/// </summary>
	// public override DbBatch CreateBatch() => new MySqlBatch();

	/// <summary>
	/// Creates a new <see cref="MySqlBatchCommand"/> object.
	/// </summary>
	// public override DbBatchCommand CreateBatchCommand() => new MySqlBatchCommand();

	/// <summary>
	/// Returns <c>true</c>.
	/// </summary>
	public override bool CanCreateBatch => false;

	/// <summary>
	/// Creates a new <see cref="MySqlDataSource"/> object.
	/// </summary>
	/// <param name="connectionString">The connection string.</param>
	// public override DbDataSource CreateDataSource(string connectionString) => new MySqlDataSource(connectionString);
#pragma warning restore CA1822 // Mark members as static

	private SqliteConnectorFactory()
	{
	}
}
