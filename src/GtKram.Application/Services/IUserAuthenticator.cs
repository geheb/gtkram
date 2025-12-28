using ErrorOr;
using GtKram.Application.UseCases.User.Models;

namespace GtKram.Application.Services;

public interface IUserAuthenticator
{
    Task<ErrorOr<AuthResult>> SignIn(string email, string password, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> SignOut(Guid id, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> UpdateEmail(Guid id, string newEmail, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> UpdatePassword(Guid id, string newPassword, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> ChangePassword(Guid id, string currentPassword, string newPassword, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> VerifyPassword(Guid id, string currentPassword, CancellationToken cancellationToken);

    Task<ErrorOr<string>> CreateConfirmRegistrationToken(Guid id, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> VerifyConfirmRegistrationToken(Guid id, string token, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> ConfirmRegistration(Guid id, string password, string token, CancellationToken cancellationToken);

    Task<ErrorOr<string>> CreateChangeEmailToken(Guid id, string newEmail, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> ConfirmChangeEmail(Guid id, string newEmail, string token, CancellationToken cancellationToken);

    Task<ErrorOr<string>> CreateResetPasswordToken(Guid id, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> VerifyResetPasswordToken(Guid id, string token, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> ConfirmResetPassword(Guid id, string newPassword, string token, CancellationToken cancellationToken);
    
    Task<ErrorOr<UserOtp>> CreateOtp(Guid id, CancellationToken cancellationToken);
    Task<ErrorOr<UserOtp>> GetOtp(Guid id, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> EnableOtp(Guid id, bool enable, string code, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> ResetOtp(Guid id, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> SignInOtp(string code, bool isRememberClient, CancellationToken cancellationToken);
}
