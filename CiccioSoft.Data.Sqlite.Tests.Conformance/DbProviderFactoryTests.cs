using AdoNet.Specification.Tests;

namespace Conformance.Tests;

public sealed class DbProviderFactoryTests(DbFactoryFixture fixture)
	: DbProviderFactoryTestBase<DbFactoryFixture>(fixture)
{
}
