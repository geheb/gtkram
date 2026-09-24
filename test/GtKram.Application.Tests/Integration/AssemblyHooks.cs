using GtKram.Application.Tests.Fixtures;

namespace GtKram.Application.Tests.Integration;

[TestClass]
public class AssemblyHooks
{
    [AssemblyInitialize]
    public static async Task Initialize(TestContext context)
    {
        await PostgresFixture.Instance.Start(context.CancellationToken);
    }

    [AssemblyCleanup]
    public static async Task Cleanup()
    {
        await PostgresFixture.Instance.DisposeAsync();
    }
}