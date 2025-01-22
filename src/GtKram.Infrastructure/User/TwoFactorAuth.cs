namespace GtKram.Infrastructure.User;

using FluentResults;
using GtKram.Application.Options;
using GtKram.Application.Services;
using GtKram.Application.UseCases.User.Models;
using GtKram.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

internal sealed class TwoFactorAuth : ITwoFactorAuth
{
    private const string _userNotFound = "Der Benutzer wurde nicht gefunden.";
    private const string _twoFactorAuthNotEnabled = "Die Zwei-Faktor-Authentifizierung (2FA) ist nicht eingerichtet.";

    private readonly IdentityErrorDescriber _errorDescriber;
    private readonly SignInManager<IdentityUserGuid> _signInManager;
    private readonly string _appTitle;

    public TwoFactorAuth(
        IOptions<AppSettings> appSettings,
        IdentityErrorDescriber errorDescriber,
        SignInManager<IdentityUserGuid> signInManager)
    {
        _appTitle = appSettings.Value.Title;
        _errorDescriber = errorDescriber;
        _signInManager = signInManager;
    }

    public async Task<Result<UserTwoFactorAuthSettings>> GetAuthenticator(Guid id)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(_userNotFound);
        }

        var key = await _signInManager.UserManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            return Result.Fail(_twoFactorAuthNotEnabled);
        }

        var isEnabled = await _signInManager.UserManager.GetTwoFactorEnabledAsync(user);

        var uri = GenerateQrCodeUri(_appTitle, user.Email!, key);

        return Result.Ok(new UserTwoFactorAuthSettings(isEnabled, key, uri));
    }

    public async Task<Result<UserTwoFactorAuthSettings>> CreateAuthenticator(Guid id)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Fail(_userNotFound);
        }

        var result = await _signInManager.UserManager.ResetAuthenticatorKeyAsync(user);
        if (!result.Succeeded)
        {
            return Result.Fail(result.Errors.Select(e => e.Description));
        }

        var key = await _signInManager.UserManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            return Result.Fail("Fehler beim Erstellen des geheimen Schlüssels.");
        }

        var isEnabled = await _signInManager.UserManager.GetTwoFactorEnabledAsync(user);

        var uri = GenerateQrCodeUri(_appTitle, user.Email!, key);

        return Result.Ok(new UserTwoFactorAuthSettings(isEnabled, key, uri));
    }

    public async Task<Result> Enable(Guid id, bool enable, string code)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return Result.Fail(_userNotFound);
        }

        var isValid = await _signInManager.UserManager.VerifyTwoFactorTokenAsync(
            user, _signInManager.UserManager.Options.Tokens.AuthenticatorTokenProvider, code);

        if (!isValid)
        {
            return Result.Fail("Der Code ist ungültig.");
        }

        var result = await _signInManager.UserManager.SetTwoFactorEnabledAsync(user, enable);
        if (!result.Succeeded)
        {
            return Result.Fail(result.Errors.Select(e => e.Description));
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
            return Result.Fail(result.Errors.Select(e => e.Description));
        }

        return Result.Ok();
    }

    public async Task<Result> Reset(Guid id)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return Result.Fail(_userNotFound);
        }

        var isEnabled = await _signInManager.UserManager.GetTwoFactorEnabledAsync(user);
        if (!isEnabled)
        {
            return Result.Fail(_twoFactorAuthNotEnabled);
        }

        var result = await _signInManager.UserManager.SetTwoFactorEnabledAsync(user, false);
        if (!result.Succeeded)
        {
            return Result.Fail(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        result = await _signInManager.UserManager.RemoveClaimAsync(user, UserClaims.TwoFactorClaim);
        if (!result.Succeeded)
        {
            return Result.Fail(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return Result.Ok();
    }

    public async Task<Result> HasUserAuthentication()
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        return user is null ? Result.Fail("Die Anmeldung is abgelaufen, bitte erneut anmelden.") : Result.Ok();
    }

    public Task<SignInResult> SignIn(string code, bool remember) => _signInManager.TwoFactorAuthenticatorSignInAsync(code, false, remember);

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
