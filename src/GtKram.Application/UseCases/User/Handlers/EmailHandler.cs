using GtKram.Domain.Base;
using GtKram.Application.Services;
using GtKram.Application.UseCases.User.Commands;
using GtKram.Domain.Repositories;
using Mediator;
using System.Web;
using Microsoft.AspNetCore.Identity;

namespace GtKram.Application.UseCases.User.Handlers;

internal sealed class EmailHandler :
    ICommandHandler<SendConfirmRegistrationCommand, Result>,
    ICommandHandler<SendChangeEmailCommand, Result>,
    ICommandHandler<SendResetPasswordCommand, Result>
{
    private readonly IdentityErrorDescriber _errorDescriber;
    private readonly IUsers _users;
    private readonly IUserAuthenticator _userAuthenticator;
    private readonly IEmailService _emailService;

    public EmailHandler(
        IdentityErrorDescriber errorDescriber,
        IUsers users,
        IUserAuthenticator userAuthenticator,
        IEmailService emailService)
    {
        _errorDescriber = errorDescriber;
        _users = users;
        _userAuthenticator = userAuthenticator;
        _emailService = emailService;
    }

    public async ValueTask<Result> Handle(SendConfirmRegistrationCommand command, CancellationToken cancellationToken)
    {
        var resultUser = await _users.FindById(command.Id, cancellationToken);
        if (resultUser.IsError)
        {
            return Result.Fail(resultUser.FirstError.Code, "error");
        }

        var resultToken = await _userAuthenticator.CreateConfirmRegistrationToken(command.Id, cancellationToken);
        if (resultToken.IsFailed)
        {
            return resultToken.ToResult();
        }

        var uriBuilder = new UriBuilder(command.CallbackUrl);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);
        query["id"] = resultUser.Value.Id.ToString();
        query["token"] = resultToken.Value;
        uriBuilder.Query = query.ToString();

        var callbackUrl = uriBuilder.ToString();

        var result = await _emailService.EnqueueConfirmRegistration(
            resultUser.Value,
            callbackUrl,
            cancellationToken);

        return result;
    }

    public async ValueTask<Result> Handle(SendChangeEmailCommand command, CancellationToken cancellationToken)
    {
        var resultUser = await _users.FindById(command.Id, cancellationToken);
        if (resultUser.IsError)
        {
            return Result.Fail(resultUser.FirstError.Code, "error");
        }

        if (!(await _users.FindByEmail(command.NewEmail, cancellationToken)).IsError)
        {
            var error = _errorDescriber.DuplicateEmail(command.NewEmail);
            return Result.Fail(error.Code, error.Description);
        }

        var resultVerify = await _userAuthenticator.VerifyPassword(command.Id, command.Password, cancellationToken);
        if (resultVerify.IsFailed)
        {
            return resultVerify;
        }

        var resultToken = await _userAuthenticator.CreateChangeEmailToken(command.Id, command.NewEmail, cancellationToken);
        if (resultToken.IsFailed)
        {
            return resultToken.ToResult();
        }

        var uriBuilder = new UriBuilder(command.CallbackUrl);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);
        query["token"] = resultToken.Value;
        uriBuilder.Query = query.ToString();

        var callbackUrl = uriBuilder.ToString();

        var user = resultUser.Value;
        user.Email = command.NewEmail; // use this new email for recipient

        var result = await _emailService.EnqueueChangeEmail(
            user,
            callbackUrl,
            cancellationToken);

        return result;
    }

    public async ValueTask<Result> Handle(SendResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var resultUser = await _users.FindByEmail(command.Email, cancellationToken);
        if (resultUser.IsError)
        {
            return Result.Fail(resultUser.FirstError.Code, "error");
        }

        var resultToken = await _userAuthenticator.CreateResetPasswordToken(resultUser.Value.Id, cancellationToken);
        if (resultToken.IsFailed)
        {
            return resultToken.ToResult();
        }

        var uriBuilder = new UriBuilder(command.CallbackUrl);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);
        query["id"] = resultUser.Value.Id.ToString();
        query["token"] = resultToken.Value;
        uriBuilder.Query = query.ToString();

        var callbackUrl = uriBuilder.ToString();

        var result = await _emailService.EnqueueResetPassword(
            resultUser.Value,
            callbackUrl,
            cancellationToken);

        return result;
    }
}
