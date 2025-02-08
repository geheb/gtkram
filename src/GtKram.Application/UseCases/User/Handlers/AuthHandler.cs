using GtKram.Domain.Base;
using GtKram.Application.Services;
using GtKram.Application.UseCases.User.Commands;
using GtKram.Application.UseCases.User.Models;
using GtKram.Application.UseCases.User.Queries;
using Mediator;
using GtKram.Domain.Errors;
using Microsoft.AspNetCore.Identity;

namespace GtKram.Application.UseCases.User.Handlers;

internal sealed class AuthHandler :
    IQueryHandler<VerifyConfirmChangePasswordQuery, Result>,
    IQueryHandler<VerifyConfirmRegistrationQuery, Result>,
    ICommandHandler<SignInCommand, Result<AuthResult>>,
    ICommandHandler<SignOutCommand, Result>,
    ICommandHandler<ConfirmRegistrationCommand, Result>,
    ICommandHandler<UpdateAuthCommand, Result>,
    ICommandHandler<ChangePasswordCommand, Result>,
    ICommandHandler<ConfirmChangeEmailCommand, Result>,
    ICommandHandler<ConfirmResetPasswordCommand, Result>,
    ICommandHandler<EnableOtpCommand, Result>,
    ICommandHandler<DisableOtpCommand, Result>,
    ICommandHandler<ResetOtpCommand, Result>,
    ICommandHandler<CreateOtpCommand, Result<UserOtp>>,
    ICommandHandler<SignInOtpCommand, Result>,
    IQueryHandler<GetOtpQuery, Result<UserOtp>>
{
    private readonly IdentityErrorDescriber _errorDescriber;
    private readonly IEmailValidatorService _emailValidatorService;
    private readonly IUserAuthenticator _userAuthenticator;

    public AuthHandler(
        IdentityErrorDescriber errorDescriber,
        IEmailValidatorService emailValidatorService,
        IUserAuthenticator userAuthenticator)
    {
        _errorDescriber = errorDescriber;
        _emailValidatorService = emailValidatorService;
        _userAuthenticator = userAuthenticator;
    }

    public async ValueTask<Result> Handle(VerifyConfirmChangePasswordQuery query, CancellationToken cancellationToken) =>
        await _userAuthenticator.VerifyResetPasswordToken(query.Id, query.Token, cancellationToken);

    public async ValueTask<Result> Handle(VerifyConfirmRegistrationQuery query, CancellationToken cancellationToken) =>
        await _userAuthenticator.VerifyConfirmRegistrationToken(query.Id, query.Token, cancellationToken);

    public async ValueTask<Result<AuthResult>> Handle(SignInCommand command, CancellationToken cancellationToken) =>
        await _userAuthenticator.SignIn(command.Email, command.Password, cancellationToken);
    public async ValueTask<Result> Handle(SignOutCommand command, CancellationToken cancellationToken) =>
        await _userAuthenticator.SignOut(command.Id, cancellationToken);

    public async ValueTask<Result> Handle(ConfirmRegistrationCommand command, CancellationToken cancellationToken) =>
        await _userAuthenticator.ConfirmRegistration(command.Id, command.Password, command.Token, cancellationToken);

    public async ValueTask<Result> Handle(UpdateAuthCommand command, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(command.Email))
        {
            if (!await _emailValidatorService.Validate(command.Email, cancellationToken))
            {
                var error = _errorDescriber.InvalidEmail(command.Email);
                return Result.Fail(error.Code, error.Description);
            }

            var result = await _userAuthenticator.UpdateEmail(command.Id, command.Email, cancellationToken);
            if (result.IsFailed)
            {
                return result;
            }
        }

        if (!string.IsNullOrWhiteSpace(command.Password))
        {
            var result = await _userAuthenticator.UpdatePassword(command.Id, command.Password, cancellationToken);
            if (result.IsFailed)
            {
                return result;
            }
        }

        return Result.Ok();
    }

    public async ValueTask<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken) =>
        await _userAuthenticator.ChangePassword(command.Id, command.CurrentPassword, command.NewPassword, cancellationToken);

    public async ValueTask<Result> Handle(ConfirmChangeEmailCommand command, CancellationToken cancellationToken) =>
        await _userAuthenticator.ConfirmChangeEmail(command.Id, command.NewEmail, command.Token, cancellationToken);

    public async ValueTask<Result> Handle(ConfirmResetPasswordCommand command, CancellationToken cancellationToken) =>
        await _userAuthenticator.ConfirmResetPassword(command.Id, command.NewPassword, command.Token, cancellationToken);

    public async ValueTask<Result> Handle(EnableOtpCommand command, CancellationToken cancellationToken) =>
        await _userAuthenticator.EnableOtp(command.Id, true, command.Code, cancellationToken);

    public async ValueTask<Result> Handle(DisableOtpCommand command, CancellationToken cancellationToken) =>
        await _userAuthenticator.EnableOtp(command.Id, false, command.Code, cancellationToken);

    public async ValueTask<Result> Handle(ResetOtpCommand command, CancellationToken cancellationToken) =>
        await _userAuthenticator.ResetOtp(command.Id, cancellationToken);

    public async ValueTask<Result<UserOtp>> Handle(CreateOtpCommand command, CancellationToken cancellationToken) =>
        await _userAuthenticator.CreateOtp(command.Id, cancellationToken);

    public async ValueTask<Result<UserOtp>> Handle(GetOtpQuery command, CancellationToken cancellationToken) =>
        await _userAuthenticator.GetOtp(command.Id, cancellationToken);

    public async ValueTask<Result> Handle(SignInOtpCommand command, CancellationToken cancellationToken) =>
        await _userAuthenticator.SignInOtp(command.Code, command.IsRememberClient, cancellationToken);
}