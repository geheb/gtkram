namespace GtKram.Core.Repositories;

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using GtKram.Core.Entities;
using GtKram.Core.Models;
using GtKram.Core.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

public sealed class TwoFactorAuth
{
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

    public async Task<Result<UserTwoFactor>> GenerateKey(Guid userId)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Result.Fail("Benutzer wurde nicht gefunden.");
        }

        var isTwoFactorEnabled = await _signInManager.IsTwoFactorEnabledAsync(user);

        var key = await _signInManager.UserManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            var result = await _signInManager.UserManager.ResetAuthenticatorKeyAsync(user);
            if (!result.Succeeded) return Result.Fail(result.Errors.Select(e => e.Description));
            key = await _signInManager.UserManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(key))
            {
                return Result.Fail(_errorDescriber.DefaultError().Description);
            }
        }

        var uri = GenerateQrCodeUri(_appTitle, user.Email!, key);

        return Result.Ok(new UserTwoFactor(isTwoFactorEnabled, key, uri));
    }

    public async Task<Result> Enable(Guid userId, bool enable, string code)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Result.Fail("Benutzer wurde nicht gefunden.");
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

    public async Task<bool> Reset(Guid userId)
    {
        var user = await _signInManager.UserManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return false;
        }

        if (!user.TwoFactorEnabled)
        {
            return true;
        }

        var result = await _signInManager.UserManager.RemoveClaimAsync(user, UserClaims.TwoFactorClaim);
        if (result.Succeeded)
        {
            result = await _signInManager.UserManager.SetTwoFactorEnabledAsync(user, false);
            return result.Succeeded;
        }

        return false;
    }

    public async Task<bool> HasUserAuthentication()
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        return user != null;
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
