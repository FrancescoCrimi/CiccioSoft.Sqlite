using AdoNet.Specification.Tests;
using Xunit;

namespace Conformance.Tests;

public sealed class ConnectionTests : ConnectionTestBase<DbFactoryFixture>
{
	public ConnectionTests(DbFactoryFixture fixture)
		: base(fixture)
	{
	}
}
