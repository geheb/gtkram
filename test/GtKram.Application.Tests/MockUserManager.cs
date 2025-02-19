using GtKram.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace GtKram.Application.Tests;

public class MockUserManager : UserManager<IdentityUserGuid>
{
    public MockUserManager()
        : base(
            Substitute.For<IUserStore<IdentityUserGuid>>(),
            Substitute.For<IOptions<IdentityOptions>>(),
            Substitute.For<IPasswordHasher<IdentityUserGuid>>(),
            [],
            [],
            Substitute.For<ILookupNormalizer>(),
            Substitute.For<IdentityErrorDescriber>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<ILogger<UserManager<IdentityUserGuid>>>())
    {
    }
}