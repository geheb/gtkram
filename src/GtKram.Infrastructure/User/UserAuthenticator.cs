namespace GtKram.Infrastructure.User;

using GtKram.Domain.Base;
using GtKram.Application.Options;
using GtKram.Application.Services;
using GtKram.Application.UseCases.User.Models;
using GtKram.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

internal sealed class UserAuthenticator : IUserAuthenticator
{
    private readonly ILogger _logger;
    private readonly IdentityErrorDescriber _errorDescriber;
    private readonly SignInManager<IdentityUserGuid> _signInManager;
    private readonly string _appTitle;

    public UserAuthenticator(
        ILogger<UserAuthenticator> logger,
        IOptions<AppSettings> appSettings,
        IdentityErrorDescriber errorDescriber,
        SignInManager<IdentityUserGuid> signInManager)
    {
        _appTitle = appSettings.Value.Title;
        _logger = logger;
        _errorDescriber = errorDescriber;
        _signInManager = signInManager;
    }

    public async Task<Result<AuthResult>> SignIn(string email, string password, CancellationToken cancellationToken)
    {
        var user = await _signInManager.UserManager.FindByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("Der Benutzer {Email} wurde nicht gefunden.", email);
            return Result.Fail(Domain.Errors.Identity.NotFound);
        }

        var result = await _signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: true);
        return await HandleResult(user, result);
    }

    public async Task<Result> SignOut(Guid id, CancellationToken cancellationToken)
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User {Id} logged out", id);
        return Result.Ok();
    }

    public async Task<Result> UpdateEmail(Guid id, string newEmail, CancellationToken cancellationToken)
    {
        var userManager = _signInManager.UserManager;
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(Domain.Errors.Identity.NotFound);
        }

        if (user.Email == newEmail)
        {
            return Result.Ok();
        }

        var hasFound = await userManager.FindByEmailAsync(newEmail) is not null;
        if (hasFound)
        {
            var error = _errorDescriber.DuplicateEmail(newEmail);
            return Result.Fail(error.Code, error.Description);
        }

        var token = await userManager.GenerateChangeEmailTokenAsync(user, newEmail);
        var result = await userManager.ChangeEmailAsync(user, newEmail, token);
        if (!result.Succeeded)
        {
            return Result.Fail(result.Errors.Select(e => (e.Code, e.Description)));
        }

        if (!user.EmailConfirmed)
        {
            token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            result = await userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                return Result.Fail(result.Errors.Select(e => (e.Code, e.Description)));
            }
        }

        return Result.Ok();
    }

    public async Task<Result> UpdatePassword(Guid id, string newPassword, CancellationToken cancellationToken)
    {
        var userManager = _signInManager.UserManager;
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(Domain.Errors.Identity.NotFound);
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            return Result.Fail(result.Errors.Select(e => (e.Code, e.Description)));
        }

        if (!user.EmailConfirmed)
        {
            token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            result = await userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                return Result.Fail(result.Errors.Select(e => (e.Code, e.Description)));
            }
        }

        return Result.Ok();
    }

    public async Task<Result> ChangePassword(Guid id, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(Domain.Errors.Identity.NotFound);
        }

        var result = _signInManager.UserManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash!, currentPassword);
        if (result != PasswordVerificationResult.Success)
        {
            var error = _errorDescriber.PasswordMismatch();
            return Result.Fail(error.Code, error.Description);
        }

        var token = await _signInManager.UserManager.GeneratePasswordResetTokenAsync(user);
        var identityResult = await _signInManager.UserManager.ResetPasswordAsync(user, token, newPassword);
        if (!identityResult.Succeeded)
        {
            return Result.Fail(identityResult.Errors.Select(e => (e.Code, e.Description)));
        }

        return Result.Ok();
    }

    public async Task<Result> VerifyPassword(Guid id, string password, CancellationToken cancellationToken)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(Domain.Errors.Identity.NotFound);
        }

        var result = _signInManager.UserManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash!, password);
        if (result != PasswordVerificationResult.Success)
        {
            var error = _errorDescriber.PasswordMismatch();
            return Result.Fail(error.Code, error.Description);
        }

        return Result.Ok();
    }

    public async Task<Result<string>> CreateConfirmRegistrationToken(Guid id, CancellationToken cancellationToken)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(Domain.Errors.Identity.NotFound);
        }

        if (user.EmailConfirmed)
        {
            return Result.Fail(Domain.Errors.Identity.AlreadyActivated);
        }

        var token = await _signInManager.UserManager.GenerateEmailConfirmationTokenAsync(user);
        return Result.Ok(token);
    }

    public async Task<Result> VerifyConfirmRegistrationToken(Guid id, string token, CancellationToken cancellationToken)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(Domain.Errors.Identity.NotFound);
        }

        var isValid = await _signInManager.UserManager.VerifyUserTokenAsync(user,
            _signInManager.UserManager.Options.Tokens.EmailConfirmationTokenProvider,
            UserManager<IdentityUserGuid>.ConfirmEmailTokenPurpose,
            token);

        if (!isValid)
        {
            var error = _errorDescriber.InvalidToken();
            return Result.Fail(error.Code, error.Description);
        }

        return Result.Ok();
    }

    public async Task<Result> ConfirmRegistration(Guid id, string password, string token, CancellationToken cancellationToken)
    {
        var userManager = _signInManager.UserManager;
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(Domain.Errors.Identity.NotFound);
        }

        var result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            return Result.Fail(result.Errors.Select(e => (e.Code, e.Description)));
        }

        token = await userManager.GeneratePasswordResetTokenAsync(user);
        result = await userManager.ResetPasswordAsync(user, token, password);
        if (!result.Succeeded)
        {
            return Result.Fail(result.Errors.Select(e => (e.Code, e.Description)));
        }

        return Result.Ok();
    }

    public async Task<Result<string>> CreateChangeEmailToken(Guid id, string newEmail, CancellationToken cancellationToken)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(Domain.Errors.Identity.NotFound);
        }

        var token = await _signInManager.UserManager.GenerateChangeEmailTokenAsync(user, newEmail);
        return Result.Ok(token);
    }

    public async Task<Result> ConfirmChangeEmail(Guid id, string newEmail, string token, CancellationToken cancellationToken)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(Domain.Errors.Identity.NotFound);
        }

        var result = await _signInManager.UserManager.ChangeEmailAsync(user, newEmail, token);
        if (!result.Succeeded)
        {
            return Result.Fail(result.Errors.Select(e => (e.Code, e.Description)));
        }

        return Result.Ok();
    }

    public async Task<Result<string>> CreateResetPasswordToken(Guid id, CancellationToken cancellationToken)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(Domain.Errors.Identity.NotFound);
        }

        var token = await _signInManager.UserManager.GeneratePasswordResetTokenAsync(user);
        return Result.Ok(token);
    }

    public async Task<Result> VerifyResetPasswordToken(Guid id, string token, CancellationToken cancellationToken)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(Domain.Errors.Identity.NotFound);
        }

        var isValid = await _signInManager.UserManager.VerifyUserTokenAsync(user,
            _signInManager.UserManager.Options.Tokens.PasswordResetTokenProvider,
            UserManager<IdentityUserGuid>.ResetPasswordTokenPurpose,
            token);

        if (!isValid)
        {
            var error = _errorDescriber.InvalidToken();
            return Result.Fail(error.Code, error.Description);
        }

        return Result.Ok();
    }

    public async Task<Result> ConfirmResetPassword(Guid id, string newPassword, string token, CancellationToken cancellationToken)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(Domain.Errors.Identity.NotFound);
        }

        var result = await _signInManager.UserManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            return Result.Fail(result.Errors.Select(e => (e.Code, e.Description)));
        }

        return Result.Ok();
    }

    public async Task<Result<UserOtp>> GetOtp(Guid id, CancellationToken cancellationToken)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(Domain.Errors.Identity.NotFound);
        }

        var key = await _signInManager.UserManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            return Result.Fail(Domain.Errors.Identity.TwoFactorNotEnabled);
        }

        var isEnabled = await _signInManager.UserManager.GetTwoFactorEnabledAsync(user);

        var uri = GenerateQrCodeUri(_appTitle, user.Email!, key);

        return Result.Ok(new UserOtp(isEnabled, key, uri));
    }

    public async Task<Result<UserOtp>> CreateOtp(Guid id, CancellationToken cancellationToken)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(Domain.Errors.Identity.NotFound);
        }

        var result = await _signInManager.UserManager.ResetAuthenticatorKeyAsync(user);
        if (!result.Succeeded)
        {
            return Result.Fail(result.Errors.Select(e => (e.Code, e.Description)));
        }

        var key = await _signInManager.UserManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            return Result.Fail(Domain.Errors.Internal.CreateKeyFailed);
        }

        var isEnabled = await _signInManager.UserManager.GetTwoFactorEnabledAsync(user);

        var uri = GenerateQrCodeUri(_appTitle, user.Email!, key);

        return Result.Ok(new UserOtp(isEnabled, key, uri));
    }

    public async Task<Result> EnableOtp(Guid id, bool enable, string code, CancellationToken cancellationToken)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return Result.Fail(Domain.Errors.Identity.NotFound);
        }

        var isValid = await _signInManager.UserManager.VerifyTwoFactorTokenAsync(
            user, _signInManager.UserManager.Options.Tokens.AuthenticatorTokenProvider, code);

        if (!isValid)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidCode);
        }

        var result = await _signInManager.UserManager.SetTwoFactorEnabledAsync(user, enable);
        if (!result.Succeeded)
        {
            return Result.Fail(result.Errors.Select(e => (e.Code, e.Description)));
        }

        if (enable)
        {
            result = await _signInManager.UserManager.AddClaimAsync(user, UserClaims.TwoFactorClaim);
        }
        else
        {
            result = await _signInManager.UserManager.RemoveClaimAsync(user, UserClaims.TwoFactorClaim);
        }

        if (!result.Succeeded)
        {
            await _signInManager.UserManager.SetTwoFactorEnabledAsync(user, false);
            return Result.Fail(result.Errors.Select(e => (e.Code, e.Description)));
        }

        return Result.Ok();
    }

    public async Task<Result> ResetOtp(Guid id, CancellationToken cancellationToken)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return Result.Fail(Domain.Errors.Identity.NotFound);
        }

        var isEnabled = await _signInManager.UserManager.GetTwoFactorEnabledAsync(user);
        if (!isEnabled)
        {
            return Result.Fail(Domain.Errors.Identity.TwoFactorNotEnabled);
        }

        var result = await _signInManager.UserManager.SetTwoFactorEnabledAsync(user, false);
        if (!result.Succeeded)
        {
            return Result.Fail(result.Errors.Select(e => (e.Code, e.Description)));
        }

        result = await _signInManager.UserManager.RemoveClaimAsync(user, UserClaims.TwoFactorClaim);
        if (!result.Succeeded)
        {
            return Result.Fail(result.Errors.Select(e => (e.Code, e.Description)));
        }

        return Result.Ok();
    }

    public async Task<Result> SignInOtp(string code, bool isRememberClient, CancellationToken cancellationToken)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            return Result.Fail(Domain.Errors.Identity.NotFound);
        }

        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(code, false, isRememberClient);
        return (await HandleResult(user, result)).ToResult();
    }

    private async Task<Result<AuthResult>> HandleResult(IdentityUserGuid user, SignInResult result)
    {
        if (result.Succeeded)
        {
            user.LastLogin = DateTimeOffset.UtcNow;
            await _signInManager.UserManager.UpdateAsync(user);
            _logger.LogInformation("Der Benutzer {Id} ist angemeldet", user.Id);
            return Result.Ok(new AuthResult(false));
        }
        else if (result.IsLockedOut)
        {
            _logger.LogWarning("Der Benutzer {Id} ist gesperrt", user.Id);
            return Result.Fail(Domain.Errors.Identity.Locked);
        }
        else if (result.IsNotAllowed)
        {
            _logger.LogWarning("Der Benutzer {Id} darf sich nicht anmelden.", user.Id);
            return Result.Fail(Domain.Errors.Identity.LoginNotAllowed);
        }
        else if (result.RequiresTwoFactor)
        {
            return Result.Ok(new AuthResult(true));
        }

        _logger.LogWarning("Die Anmeldung für Benutzer {Id} ist fehlgeschlagen.", user.Id);
        return Result.Fail(Domain.Errors.Identity.LoginFailed);
    }

    private static string GenerateQrCodeUri(string issuer, string user, string secret)
    {
        var dictionary = new Dictionary<string, string>
        {
            { "secret", secret },
            { "issuer", Uri.EscapeDataString(issuer) },
            { "algorithm","SHA1" },
            { "digits", "6" },
            { "period", "30" }
        };

        var stringBuilder = new StringBuilder("otpauth://totp/");
        stringBuilder.Append(Uri.EscapeDataString(issuer));
        stringBuilder.Append(':');
        stringBuilder.Append(Uri.EscapeDataString(user));
        stringBuilder.Append('?');
        foreach (var item in dictionary)
        {
            stringBuilder.Append(item.Key);
            stringBuilder.Append('=');
            stringBuilder.Append(item.Value);
            stringBuilder.Append('&');
        }

        stringBuilder.Remove(stringBuilder.Length - 1, 1);
        return stringBuilder.ToString();
    }
}
