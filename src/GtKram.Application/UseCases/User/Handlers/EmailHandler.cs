using FluentResults;
using GtKram.Application.Services;
using GtKram.Application.UseCases.User.Commands;
using GtKram.Domain.Repositories;
using Mediator;
using System.Web;

namespace GtKram.Application.UseCases.User.Handlers;

internal sealed class EmailHandler :
    ICommandHandler<SendConfirmRegistrationCommand, Result>,
    ICommandHandler<SendChangeEmailCommand, Result>,
    ICommandHandler<SendResetPasswordCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserAuthenticator _userAuthenticator;
    private readonly IEmailValidatorService _emailValidatorService;
    private readonly IEmailService _emailService;

    public EmailHandler(
        IUserRepository userRepository,
        IUserAuthenticator userAuthenticator,
        IEmailValidatorService emailValidatorService,
        IEmailService emailService)
    {
        _userRepository = userRepository;
        _userAuthenticator = userAuthenticator;
        _emailValidatorService = emailValidatorService;
        _emailService = emailService;
    }

    public async ValueTask<Result> Handle(SendConfirmRegistrationCommand command, CancellationToken cancellationToken)
    {
        var resultUser = await _userRepository.FindById(command.Id, cancellationToken);
        if (resultUser.IsFailed)
        {
            return resultUser.ToResult();
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
        var resultUser = await _userRepository.FindById(command.Id, cancellationToken);
        if (resultUser.IsFailed)
        {
            return resultUser.ToResult();
        }

        if ((await _userRepository.FindByEmail(command.NewEmail, cancellationToken)).IsSuccess)
        {
            return Result.Fail("Die neue E-Mail-Adresse ist bereits vergeben.");
        }

        if (!await _emailValidatorService.Validate(command.NewEmail , cancellationToken))
        {
            return Result.Fail("Die neue E-Mail-Adresse ist ungültig.");
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
        var resultUser = await _userRepository.FindByEmail(command.Email, cancellationToken);
        if (resultUser.IsFailed)
        {
            return resultUser.ToResult();
        }

        var resultToken = await _userAuthenticator.CreateResetPasswordToken(resultUser.Value.Id, cancellationToken);
        if (resultToken.IsFailed)
        {
            return resultToken.ToResult();
        }

        var uriBuilder = new UriBuilder(command.CallbackUrl);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);
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
