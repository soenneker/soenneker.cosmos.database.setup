using Soenneker.Cosmos.Database.Setup.Abstract;
using Soenneker.Tests.HostedUnit;

namespace Soenneker.Cosmos.Database.Setup.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public class CosmosDatabaseSetupUtilTests : HostedUnitTest
{
    private readonly ICosmosDatabaseSetupUtil _util;

    public CosmosDatabaseSetupUtilTests(Host host) : base(host)
    {
        _util = Resolve<ICosmosDatabaseSetupUtil>(true);
    }

    [Test]
    public void Default()
    {

    }
}
