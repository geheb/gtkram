using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Persistence.Entities;
using GtKram.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace GtKram.Application.Tests.Integration;

public class UserTests : DatabaseFixture
{
    protected override void Setup(IServiceCollection services)
    {
        var mockUserManager = Substitute.For<MockUserManager>();
        mockUserManager.CreateAsync(Arg.Any<IdentityUserGuid>()).Returns(IdentityResult.Success);
        mockUserManager.AddToRolesAsync(Arg.Any<IdentityUserGuid>(), Arg.Any<IEnumerable<string>>()).Returns(IdentityResult.Success);

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<UserManager<IdentityUserGuid>>(s => mockUserManager);
        services.AddScoped(s => Substitute.For<IdentityErrorDescriber>());
        services.AddScoped<IUserRepository, UserRepository>();
    }

    [Fact]
    public async Task Create_User_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var result = await userRepo.Create("foo", "foo@bar", [UserRoleType.Manager], default);
        result.IsSuccess.ShouldBeTrue();
    }
}
