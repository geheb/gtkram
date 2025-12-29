using ErrorOr;
using GtKram.Application.Services;
using GtKram.Application.UseCases.User.Commands;
using GtKram.Application.UseCases.User.Models;
using GtKram.Application.UseCases.User.Queries;
using Mediator;

namespace GtKram.Application.UseCases.User.Handlers;

internal sealed class AuthHandler :
    IQueryHandler<VerifyConfirmChangePasswordQuery, ErrorOr<Success>>,
    IQueryHandler<VerifyConfirmRegistrationQuery, ErrorOr<Success>>,
    ICommandHandler<SignInCommand, ErrorOr<AuthResult>>,
    ICommandHandler<SignOutCommand, ErrorOr<Success>>,
    ICommandHandler<ConfirmRegistrationCommand, ErrorOr<Success>>,
    ICommandHandler<UpdateAuthCommand, ErrorOr<Success>>,
    ICommandHandler<ChangePasswordCommand, ErrorOr<Success>>,
    ICommandHandler<ConfirmChangeEmailCommand, ErrorOr<Success>>,
    ICommandHandler<ConfirmResetPasswordCommand, ErrorOr<Success>>,
    ICommandHandler<EnableOtpCommand, ErrorOr<Success>>,
    ICommandHandler<DisableOtpCommand, ErrorOr<Success>>,
    ICommandHandler<ResetOtpCommand, ErrorOr<Success>>,
    ICommandHandler<CreateOtpCommand, ErrorOr<UserOtp>>,
    ICommandHandler<SignInOtpCommand, ErrorOr<Success>>,
    IQueryHandler<GetOtpQuery, ErrorOr<UserOtp>>
{
    private readonly IUserAuthenticator _userAuthenticator;

    public AuthHandler(
        IUserAuthenticator userAuthenticator)
    {
        _userAuthenticator = userAuthenticator;
    }

    public async ValueTask<ErrorOr<Success>> Handle(VerifyConfirmChangePasswordQuery query, CancellationToken cancellationToken) =>
        await _userAuthenticator.VerifyResetPasswordToken(query.Id, query.Token, cancellationToken);

    public async ValueTask<ErrorOr<Success>> Handle(VerifyConfirmRegistrationQuery query, CancellationToken cancellationToken) =>
        await _userAuthenticator.VerifyConfirmRegistrationToken(query.Id, query.Token, cancellationToken);

    public async ValueTask<ErrorOr<AuthResult>> Handle(SignInCommand command, CancellationToken cancellationToken) =>
        await _userAuthenticator.SignIn(command.Email, command.Password, cancellationToken);

    public async ValueTask<ErrorOr<Success>> Handle(SignOutCommand command, CancellationToken cancellationToken) =>
        await _userAuthenticator.SignOut(command.Id, cancellationToken);

    public async ValueTask<ErrorOr<Success>> Handle(ConfirmRegistrationCommand command, CancellationToken cancellationToken) =>
        await _userAuthenticator.ConfirmRegistration(command.Id, command.Password, command.Token, cancellationToken);

    public async ValueTask<ErrorOr<Success>> Handle(UpdateAuthCommand command, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(command.Email))
        {
            var result = await _userAuthenticator.UpdateEmail(command.Id, command.Email, cancellationToken);
            if (result.IsError)
            {
                return result;
            }
        }

        if (!string.IsNullOrWhiteSpace(command.Password))
        {
            var result = await _userAuthenticator.UpdatePassword(command.Id, command.Password, cancellationToken);
            if (result.IsError)
            {
                return result;
            }
        }

        return Result.Success;
    }

    public async ValueTask<ErrorOr<Success>> Handle(ChangePasswordCommand command, CancellationToken cancellationToken) =>
        await _userAuthenticator.ChangePassword(command.Id, command.CurrentPassword, command.NewPassword, cancellationToken);

    public async ValueTask<ErrorOr<Success>> Handle(ConfirmChangeEmailCommand command, CancellationToken cancellationToken) =>
        await _userAuthenticator.ConfirmChangeEmail(command.Id, command.NewEmail, command.Token, cancellationToken);

    public async ValueTask<ErrorOr<Success>> Handle(ConfirmResetPasswordCommand command, CancellationToken cancellationToken) =>
        await _userAuthenticator.ConfirmResetPassword(command.Id, command.NewPassword, command.Token, cancellationToken);

    public async ValueTask<ErrorOr<Success>> Handle(EnableOtpCommand command, CancellationToken cancellationToken) =>
        await _userAuthenticator.EnableOtp(command.Id, true, command.Code, cancellationToken);

    public async ValueTask<ErrorOr<Success>> Handle(DisableOtpCommand command, CancellationToken cancellationToken) =>
        await _userAuthenticator.EnableOtp(command.Id, false, command.Code, cancellationToken);

    public async ValueTask<ErrorOr<Success>> Handle(ResetOtpCommand command, CancellationToken cancellationToken) =>
        await _userAuthenticator.ResetOtp(command.Id, cancellationToken);

    public async ValueTask<ErrorOr<UserOtp>> Handle(CreateOtpCommand command, CancellationToken cancellationToken) =>
        await _userAuthenticator.CreateOtp(command.Id, cancellationToken);

    public async ValueTask<ErrorOr<UserOtp>> Handle(GetOtpQuery command, CancellationToken cancellationToken) =>
        await _userAuthenticator.GetOtp(command.Id, cancellationToken);

    public async ValueTask<ErrorOr<Success>> Handle(SignInOtpCommand command, CancellationToken cancellationToken) =>
        await _userAuthenticator.SignInOtp(command.Code, command.IsRememberClient, cancellationToken);
}
