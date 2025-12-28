using ErrorOr;
using GtKram.Application.Services;
using GtKram.Application.UseCases.User.Commands;
using GtKram.Application.UseCases.User.Extensions;
using GtKram.Domain.Repositories;
using Mediator;
using Microsoft.AspNetCore.Identity;
using System.Web;

namespace GtKram.Application.UseCases.User.Handlers;

internal sealed class EmailHandler :
    ICommandHandler<SendConfirmRegistrationCommand, ErrorOr<Success>>,
    ICommandHandler<SendChangeEmailCommand, ErrorOr<Success>>,
    ICommandHandler<SendResetPasswordCommand, ErrorOr<Success>>
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

    public async ValueTask<ErrorOr<Success>> Handle(SendConfirmRegistrationCommand command, CancellationToken cancellationToken)
    {
        var resultUser = await _users.FindById(command.Id, cancellationToken);
        if (resultUser.IsError)
        {
            return resultUser.Errors;
        }

        var resultToken = await _userAuthenticator.CreateConfirmRegistrationToken(command.Id, cancellationToken);
        if (resultToken.IsError)
        {
            return resultToken.Errors;
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

    public async ValueTask<ErrorOr<Success>> Handle(SendChangeEmailCommand command, CancellationToken cancellationToken)
    {
        var resultUser = await _users.FindById(command.Id, cancellationToken);
        if (resultUser.IsError)
        {
            return resultUser.Errors;
        }

        if (!(await _users.FindByEmail(command.NewEmail, cancellationToken)).IsError)
        {
            return _errorDescriber.DuplicateEmail(command.NewEmail).ToError();
        }

        var resultVerify = await _userAuthenticator.VerifyPassword(command.Id, command.Password, cancellationToken);
        if (resultVerify.IsError)
        {
            return resultVerify;
        }

        var resultToken = await _userAuthenticator.CreateChangeEmailToken(command.Id, command.NewEmail, cancellationToken);
        if (resultToken.IsError)
        {
            return resultToken.Errors;
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

    public async ValueTask<ErrorOr<Success>> Handle(SendResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var resultUser = await _users.FindByEmail(command.Email, cancellationToken);
        if (resultUser.IsError)
        {
            return resultUser.Errors;
        }

        var resultToken = await _userAuthenticator.CreateResetPasswordToken(resultUser.Value.Id, cancellationToken);
        if (resultToken.IsError)
        {
            return resultToken.Errors;
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
