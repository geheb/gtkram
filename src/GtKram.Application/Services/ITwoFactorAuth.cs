using FluentResults;
using GtKram.Application.UseCases.User.Models;
using Microsoft.AspNetCore.Identity;

namespace GtKram.Application.Services;

public interface ITwoFactorAuth
{
    Task<Result<UserTwoFactor>> GenerateKey(Guid userId);
    Task<Result> Enable(Guid userId, bool enable, string code);
    Task<bool> Reset(Guid userId);
    Task<bool> HasUserAuthentication();
    Task<SignInResult> SignIn(string code, bool remember);
}
