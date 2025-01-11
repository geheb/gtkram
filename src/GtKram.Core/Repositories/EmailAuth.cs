namespace GtKram.Core.Repositories;

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using GtKram.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

public sealed class EmailAuth
{
    private readonly ILogger _logger;
    private readonly SignInManager<IdentityUserGuid> _signInManager;

    public EmailAuth(
        ILogger<EmailAuth> logger,
        SignInManager<IdentityUserGuid> signInManager)
    {
        _logger = logger;
        _signInManager = signInManager;
    }

    public async Task<SignInResult> SignIn(string email, string password)
    {
        var user = await _signInManager.UserManager.FindByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("User {Email} not found", email);
            return SignInResult.Failed;
        }

        var result = await _signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            user.LastLogin = DateTimeOffset.UtcNow;
            await _signInManager.UserManager.UpdateAsync(user);
            _logger.LogInformation("User {Email} logged in", email);
        }
        else if (result.IsLockedOut)
        {
            _logger.LogWarning("User {Email} has locked out", email);
        }
        else if (result.IsNotAllowed)
        {
            _logger.LogWarning("User {Email} is not allowed to login", email);
        }
        else if (!result.RequiresTwoFactor)
        {
            _logger.LogWarning("User {Email} failed to log in", email);
        }

        return result;
    }

    public async Task SignOut(ClaimsPrincipal principal)
    {
        var email = principal.FindFirstValue(ClaimTypes.Email);

        await _signInManager.SignOutAsync();

        _logger.LogInformation("User {Email} logged out", email);
    }
}
