using FluentResults;
using GtKram.Application.UseCases.User.Models;

namespace GtKram.Application.Services;

public interface IUserAuthenticator
{
    Task<Result<AuthResult>> SignIn(string email, string password, CancellationToken cancellationToken);
    Task<Result> SignOut(Guid id, CancellationToken cancellationToken);
    Task<Result> UpdateEmail(Guid id, string newEmail, CancellationToken cancellationToken);
    Task<Result> UpdatePassword(Guid id, string newPassword, CancellationToken cancellationToken);
    Task<Result> ChangePassword(Guid id, string currentPassword, string newPassword, CancellationToken cancellationToken);
    Task<Result> VerifyPassword(Guid id, string currentPassword, CancellationToken cancellationToken);

    Task<Result<string>> CreateConfirmRegistrationToken(Guid id, CancellationToken cancellationToken);
    Task<Result> VerifyConfirmRegistrationToken(Guid id, string token, CancellationToken cancellationToken);
    Task<Result> ConfirmRegistration(Guid id, string password, string token, CancellationToken cancellationToken);

    Task<Result<string>> CreateChangeEmailToken(Guid id, string newEmail, CancellationToken cancellationToken);
    Task<Result> ConfirmChangeEmail(Guid id, string newEmail, string token, CancellationToken cancellationToken);

    Task<Result<string>> CreateResetPasswordToken(Guid id, CancellationToken cancellationToken);
    Task<Result> VerifyResetPasswordToken(Guid id, string token, CancellationToken cancellationToken);
    Task<Result> ConfirmResetPassword(Guid id, string newPassword, string token, CancellationToken cancellationToken);
    
    Task<Result<UserOtp>> CreateOtp(Guid id, CancellationToken cancellationToken);
    Task<Result<UserOtp>> GetOtp(Guid id, CancellationToken cancellationToken);
    Task<Result> EnableOtp(Guid id, bool enable, string code, CancellationToken cancellationToken);
    Task<Result> ResetOtp(Guid id, CancellationToken cancellationToken);
    Task<Result> SignInOtp(string code, bool isRememberClient, CancellationToken cancellationToken);
}
