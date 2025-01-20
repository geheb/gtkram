namespace GtKram.Infrastructure.User;

using GtKram.Application.Services;
using GtKram.Application.UseCases.User.Extensions;
using GtKram.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

internal sealed class EmailAuth : IEmailAuth
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
            _logger.LogInformation("User {Id} logged in", user.Id);
        }
        else if (result.IsLockedOut)
        {
            _logger.LogWarning("User {Id} has locked out", user.Id);
        }
        else if (result.IsNotAllowed)
        {
            _logger.LogWarning("User {Id} is not allowed to login", user.Id);
        }
        else if (!result.RequiresTwoFactor)
        {
            _logger.LogWarning("User {Id} failed to log in", user.Id);
        }

        return result;
    }

    public async Task SignOut(ClaimsPrincipal principal)
    {
        var id = principal.GetId();

        await _signInManager.SignOutAsync();

        _logger.LogInformation("User {Id} logged out", id);
    }
}
