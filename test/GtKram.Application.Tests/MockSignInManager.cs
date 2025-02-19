using GtKram.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace GtKram.Application.Tests;

public class MockSignInManager : SignInManager<IdentityUserGuid>
{
    public MockSignInManager()
        : base(
            Substitute.For<MockUserManager>(),
            new HttpContextAccessor(),
            Substitute.For<IUserClaimsPrincipalFactory<IdentityUserGuid>>(),
            Substitute.For<IOptions<IdentityOptions>>(),
            Substitute.For<ILogger<SignInManager<IdentityUserGuid>>>(),
            Substitute.For<IAuthenticationSchemeProvider>(),
            Substitute.For<IUserConfirmation<IdentityUserGuid>>())
    {
    }
}