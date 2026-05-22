using AdoNet.Specification.Tests;
using Xunit;

namespace Conformance.Tests;

public sealed class ParameterTests : ParameterTestBase<DbFactoryFixture>
{
	public ParameterTests(DbFactoryFixture fixture)
		: base(fixture)
	{
	}
}
