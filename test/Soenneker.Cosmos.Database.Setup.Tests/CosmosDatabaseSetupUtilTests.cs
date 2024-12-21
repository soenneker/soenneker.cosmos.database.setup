using Soenneker.Cosmos.Database.Setup.Abstract;
using Soenneker.Tests.FixturedUnit;
using Xunit;


namespace Soenneker.Cosmos.Database.Setup.Tests;

[Collection("Collection")]
public class CosmosDatabaseSetupUtilTests : FixturedUnitTest
{
    private readonly ICosmosDatabaseSetupUtil _util;

    public CosmosDatabaseSetupUtilTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _util = Resolve<ICosmosDatabaseSetupUtil>(true);
    }

    [Fact]
    public void Default()
    {

    }
}
