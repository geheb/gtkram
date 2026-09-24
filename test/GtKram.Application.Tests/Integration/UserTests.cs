using GtKram.Application.Tests.Fixtures;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace GtKram.Application.Tests.Integration;

[TestClass]
public sealed class UserTests
{
    private ServiceFixture _fixture = null!;
    private CancellationToken _cancellationToken;

    public UserTests(TestContext context)
    {
        _cancellationToken = context.CancellationToken;
    }

    [TestInitialize]
    public async Task Init()
    {
        _fixture = new(await PostgresFixture.Instance.CreateDatabase());

        _fixture.Build();

        await _fixture.MigrateDb(_cancellationToken);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _fixture.DisposeAsync();
    }

    [TestMethod]
    public async Task Create_User_IsSuccess()
    {
        await using var scope = _fixture.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUsers>();

        var result = await users.Create("foo", "foo@bar", [UserRoleType.Manager], _cancellationToken);
        result.IsError.ShouldBeFalse();
    }
}
