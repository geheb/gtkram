using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Persistence.Entities;
using GtKram.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace GtKram.Application.Tests.Integration;

[TestClass]
public class UserTests
{
    private readonly ServiceFixture _fixture = new();
    private IServiceProvider _serviceProvider = null!;

    [TestInitialize]
    public void Init()
    {
        var mockUserManager = Substitute.For<MockUserManager>();
        mockUserManager.CreateAsync(Arg.Any<IdentityUserGuid>()).Returns(IdentityResult.Success);
        mockUserManager.AddToRolesAsync(Arg.Any<IdentityUserGuid>(), Arg.Any<IEnumerable<string>>()).Returns(IdentityResult.Success);

        _fixture.Services.AddSingleton(TimeProvider.System);
        _fixture.Services.AddScoped<UserManager<IdentityUserGuid>>(s => mockUserManager);
        _fixture.Services.AddScoped(s => Substitute.For<IdentityErrorDescriber>());
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

        var result = await userRepo.Create("foo", "foo@bar", [UserRoleType.Manager], default);
        result.IsSuccess.ShouldBeTrue();
    }
}
