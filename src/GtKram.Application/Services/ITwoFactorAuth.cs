using FluentResults;
using GtKram.Application.UseCases.User.Models;
using Microsoft.AspNetCore.Identity;

namespace GtKram.Application.Services;

public interface ITwoFactorAuth
{
    Task<Result<UserTwoFactorAuthSettings>> CreateAuthenticator(Guid id);
    Task<Result<UserTwoFactorAuthSettings>> GetAuthenticator(Guid id);
    Task<Result> Enable(Guid id, bool enable, string code);
    Task<Result> Reset(Guid id);
    Task<Result> HasUserAuthentication();
    Task<SignInResult> SignIn(string code, bool remember);
}
