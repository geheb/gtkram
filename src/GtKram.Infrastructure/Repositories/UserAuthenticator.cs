namespace GtKram.Infrastructure.Repositories;

using ErrorOr;
using GtKram.Application.Options;
using GtKram.Application.Services;
using GtKram.Application.UseCases.User.Extensions;
using GtKram.Application.UseCases.User.Models;
using GtKram.Infrastructure.Database;
using GtKram.Infrastructure.Database.Models;
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
    private readonly TimeProvider _timeProvider;
    private readonly IEmailValidatorService _emailValidator;
    private readonly IdentityErrorDescriber _errorDescriber;
    private readonly SignInManager<Identity> _signInManager;
    private readonly string _appTitle;

    public UserAuthenticator(
        ILogger<UserAuthenticator> logger,
        TimeProvider timeProvider,
        IOptions<AppSettings> appSettings,
        IEmailValidatorService emailValidator,
        IdentityErrorDescriber errorDescriber,
        SignInManager<Identity> signInManager)
    {
        _appTitle = appSettings.Value.Title;
        _logger = logger;
        _timeProvider = timeProvider;
        _emailValidator = emailValidator;
        _errorDescriber = errorDescriber;
        _signInManager = signInManager;
    }

    public async Task<ErrorOr<AuthResult>> SignIn(string email, string password, CancellationToken cancellationToken)
    {
        var user = await _signInManager.UserManager.FindByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("Der Benutzer {Email} wurde nicht gefunden.", email);
            return Domain.Errors.Identity.NotFound;
        }

        var result = await _signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: true);
        return await HandleResult(user, result);
    }

    public async Task<ErrorOr<Success>> SignOut(Guid id, CancellationToken cancellationToken)
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User {Id} logged out", id);
        return Result.Success;
    }

    public async Task<ErrorOr<Success>> UpdateEmail(Guid id, string newEmail, CancellationToken cancellationToken)
    {
        var userManager = _signInManager.UserManager;
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Domain.Errors.Identity.NotFound;
        }

        if (user.Email == newEmail)
        {
            return Result.Success;
        }

        if (!await _emailValidator.Validate(newEmail, cancellationToken))
        {
            return _errorDescriber.InvalidEmail(newEmail).ToError();
        }

        var hasFound = await userManager.FindByEmailAsync(newEmail) is not null;
        if (hasFound)
        {
            return _errorDescriber.DuplicateEmail(newEmail).ToError();
        }

        var token = await userManager.GenerateChangeEmailTokenAsync(user, newEmail);
        var result = await userManager.ChangeEmailAsync(user, newEmail, token);
        if (!result.Succeeded)
        {
            return result.Errors.ToError();
        }

        if (!user.Json.IsEmailConfirmed)
        {
            token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            result = await userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                return result.Errors.ToError();
            }
        }

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> UpdatePassword(Guid id, string newPassword, CancellationToken cancellationToken)
    {
        var userManager = _signInManager.UserManager;
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Domain.Errors.Identity.NotFound;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            return result.Errors.ToError();
        }

        if (!user.Json.IsEmailConfirmed)
        {
            token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            result = await userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                return result.Errors.ToError();
            }
        }

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> ChangePassword(Guid id, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Domain.Errors.Identity.NotFound;
        }

        var result = _signInManager.UserManager.PasswordHasher.VerifyHashedPassword(user, user.Json.PasswordHash!, currentPassword);
        if (result != PasswordVerificationResult.Success)
        {
            return _errorDescriber.PasswordMismatch().ToError();
        }

        var token = await _signInManager.UserManager.GeneratePasswordResetTokenAsync(user);
        var identityResult = await _signInManager.UserManager.ResetPasswordAsync(user, token, newPassword);
        if (!identityResult.Succeeded)
        {
            return identityResult.Errors.ToError();
        }

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> VerifyPassword(Guid id, string password, CancellationToken cancellationToken)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Domain.Errors.Identity.NotFound;
        }

        var result = _signInManager.UserManager.PasswordHasher.VerifyHashedPassword(user, user.Json.PasswordHash!, password);
        if (result != PasswordVerificationResult.Success)
        {
            return _errorDescriber.PasswordMismatch().ToError();
        }

        return Result.Success;
    }

    public async Task<ErrorOr<string>> CreateConfirmRegistrationToken(Guid id, CancellationToken cancellationToken)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Domain.Errors.Identity.NotFound;
        }

        if (user.Json.IsEmailConfirmed)
        {
            return Domain.Errors.Identity.AlreadyActivated;
        }

        var token = await _signInManager.UserManager.GenerateEmailConfirmationTokenAsync(user);
        return token;
    }

    public async Task<ErrorOr<Success>> VerifyConfirmRegistrationToken(Guid id, string token, CancellationToken cancellationToken)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Domain.Errors.Identity.NotFound;
        }

        var isValid = await _signInManager.UserManager.VerifyUserTokenAsync(user,
            _signInManager.UserManager.Options.Tokens.EmailConfirmationTokenProvider,
            UserManager<Identity>.ConfirmEmailTokenPurpose,
            token);

        if (!isValid)
        {
            return _errorDescriber.InvalidToken().ToError();
        }

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> ConfirmRegistration(Guid id, string password, string token, CancellationToken cancellationToken)
    {
        var userManager = _signInManager.UserManager;
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Domain.Errors.Identity.NotFound;
        }

        var result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            return result.Errors.ToError();
        }

        token = await userManager.GeneratePasswordResetTokenAsync(user);
        result = await userManager.ResetPasswordAsync(user, token, password);
        if (!result.Succeeded)
        {
            return result.Errors.ToError();
        }

        return Result.Success;
    }

    public async Task<ErrorOr<string>> CreateChangeEmailToken(Guid id, string newEmail, CancellationToken cancellationToken)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Domain.Errors.Identity.NotFound;
        }

        if (!await _emailValidator.Validate(newEmail, cancellationToken))
        {
            return _errorDescriber.InvalidEmail(newEmail).ToError();
        }

        var token = await _signInManager.UserManager.GenerateChangeEmailTokenAsync(user, newEmail);
        return token;
    }

    public async Task<ErrorOr<Success>> ConfirmChangeEmail(Guid id, string newEmail, string token, CancellationToken cancellationToken)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Domain.Errors.Identity.NotFound;
        }

        var result = await _signInManager.UserManager.ChangeEmailAsync(user, newEmail, token);
        if (!result.Succeeded)
        {
            return result.Errors.ToError();
        }

        return Result.Success;
    }

    public async Task<ErrorOr<string>> CreateResetPasswordToken(Guid id, CancellationToken cancellationToken)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Domain.Errors.Identity.NotFound;
        }

        var token = await _signInManager.UserManager.GeneratePasswordResetTokenAsync(user);
        return token;
    }

    public async Task<ErrorOr<Success>> VerifyResetPasswordToken(Guid id, string token, CancellationToken cancellationToken)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Domain.Errors.Identity.NotFound;
        }

        var isValid = await _signInManager.UserManager.VerifyUserTokenAsync(user,
            _signInManager.UserManager.Options.Tokens.PasswordResetTokenProvider,
            UserManager<Identity>.ResetPasswordTokenPurpose,
            token);

        if (!isValid)
        {
            return _errorDescriber.InvalidToken().ToError();
        }

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> ConfirmResetPassword(Guid id, string newPassword, string token, CancellationToken cancellationToken)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Domain.Errors.Identity.NotFound;
        }

        var result = await _signInManager.UserManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            return result.Errors.ToError();
        }

        return Result.Success;
    }

    public async Task<ErrorOr<UserOtp>> GetOtp(Guid id, CancellationToken cancellationToken)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Domain.Errors.Identity.NotFound;
        }

        var key = await _signInManager.UserManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            return Domain.Errors.Identity.TwoFactorNotEnabled;
        }

        var isEnabled = await _signInManager.UserManager.GetTwoFactorEnabledAsync(user);

        var uri = GenerateQrCodeUri(_appTitle, user.Email!, key);

        return new UserOtp(isEnabled, key, uri);
    }

    public async Task<ErrorOr<UserOtp>> CreateOtp(Guid id, CancellationToken cancellationToken)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Domain.Errors.Identity.NotFound;
        }

        var result = await _signInManager.UserManager.ResetAuthenticatorKeyAsync(user);
        if (!result.Succeeded)
        {
            return result.Errors.ToError();
        }

        var key = await _signInManager.UserManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            return Domain.Errors.Internal.CreateKeyFailed;
        }

        var isEnabled = await _signInManager.UserManager.GetTwoFactorEnabledAsync(user);

        var uri = GenerateQrCodeUri(_appTitle, user.Email!, key);

        return new UserOtp(isEnabled, key, uri);
    }

    public async Task<ErrorOr<Success>> EnableOtp(Guid id, bool enable, string code, CancellationToken cancellationToken)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return Domain.Errors.Identity.NotFound;
        }

        var isValid = await _signInManager.UserManager.VerifyTwoFactorTokenAsync(
            user, _signInManager.UserManager.Options.Tokens.AuthenticatorTokenProvider, code);

        if (!isValid)
        {
            return Domain.Errors.Internal.InvalidCode;
        }

        var result = await _signInManager.UserManager.SetTwoFactorEnabledAsync(user, enable);
        if (!result.Succeeded)
        {
            return result.Errors.ToError();
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
            return result.Errors.ToError();
        }

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> ResetOtp(Guid id, CancellationToken cancellationToken)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return Domain.Errors.Identity.NotFound;
        }

        var isEnabled = await _signInManager.UserManager.GetTwoFactorEnabledAsync(user);
        if (!isEnabled)
        {
            return Domain.Errors.Identity.TwoFactorNotEnabled;
        }

        var result = await _signInManager.UserManager.SetTwoFactorEnabledAsync(user, false);
        if (!result.Succeeded)
        {
            return result.Errors.ToError();
        }

        result = await _signInManager.UserManager.RemoveClaimAsync(user, UserClaims.TwoFactorClaim);
        if (!result.Succeeded)
        {
            return result.Errors.ToError();
        }

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> SignInOtp(string code, bool isRememberClient, CancellationToken cancellationToken)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            return Domain.Errors.Identity.NotFound;
        }

        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(code, false, isRememberClient);
        var auth = await HandleResult(user, result);
        return auth.IsError ? auth.Errors : Result.Success;
    }

    private async Task<ErrorOr<AuthResult>> HandleResult(Identity user, SignInResult result)
    {
        if (result.Succeeded)
        {
            user.Json.LastLogin = _timeProvider.GetUtcNow();
            await _signInManager.UserManager.UpdateAsync(user);
            _logger.LogInformation("Der Benutzer {Id} ist angemeldet", user.Id);
            return new AuthResult(false);
        }
        else if (result.IsLockedOut)
        {
            _logger.LogWarning("Der Benutzer {Id} ist gesperrt", user.Id);
            return Domain.Errors.Identity.Locked;
        }
        else if (result.IsNotAllowed)
        {
            _logger.LogWarning("Der Benutzer {Id} darf sich nicht anmelden.", user.Id);
            return Domain.Errors.Identity.LoginNotAllowed;
        }
        else if (result.RequiresTwoFactor)
        {
            return new AuthResult(true);
        }

        _logger.LogWarning("Die Anmeldung für Benutzer {Id} ist fehlgeschlagen.", user.Id);
        return Domain.Errors.Identity.LoginFailed;
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
