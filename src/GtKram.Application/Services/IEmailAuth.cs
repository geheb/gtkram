using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace GtKram.Application.Services;

public interface IEmailAuth
{
    Task<SignInResult> SignIn(string email, string password);
    Task SignOut(ClaimsPrincipal principal);
}
