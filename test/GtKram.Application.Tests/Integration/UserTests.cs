using FluentMigrator.Runner;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace GtKram.Application.Tests.Integration;

[TestClass]
public class UserTests
{
    private readonly ServiceFixture _fixture = new();
    private IServiceProvider _serviceProvider = null!;
    private CancellationToken _cancellationToken;

    public UserTests(TestContext context)
    {
        _cancellationToken = context.CancellationTokenSource.Token;
    }

    [TestInitialize]
    public async Task Init()
    {
        _fixture.Services.AddScoped<IUsers, Users>();
        _serviceProvider = _fixture.Build();

        await using var scope = _serviceProvider.CreateAsyncScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _fixture.DisposeAsync();
    }

    [TestMethod]
    public async Task Create_User_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<IUsers>();

        var result = await users.Create("foo", "foo@bar", [UserRoleType.Manager], _cancellationToken);
        result.IsSuccess.ShouldBeTrue();
    }
}
