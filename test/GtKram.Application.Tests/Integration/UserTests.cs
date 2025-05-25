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
    public void Init()
    {
        _fixture.Services.AddScoped<IUserRepository, UserRepository>();
        _serviceProvider = _fixture.Build();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _fixture.Dispose();
    }

    [TestMethod]
    public async Task Create_User_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var result = await userRepo.Create("foo", "foo@bar", [UserRoleType.Manager], _cancellationToken);
        result.IsSuccess.ShouldBeTrue();
    }
}
