using FluentResults;
using GtKram.Application.Services;
using GtKram.Application.UseCases.User.Commands;
using GtKram.Domain.Repositories;
using Mediator;
using Microsoft.AspNetCore.Identity;
using System.Web;

namespace GtKram.Application.UseCases.User.Handler;

internal sealed class EnqueueEmailHandler :
    ICommandHandler<SendConfirmRegistrationCommand, Result>,
    ICommandHandler<SendChangeEmailCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailValidatorService _emailValidatorService;
    private readonly IEmailService _emailService;

    public EnqueueEmailHandler(
        IUserRepository userRepository,
        IEmailValidatorService emailValidatorService,
        IEmailService emailService)
    {
        _userRepository = userRepository;
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

        var resultToken = await _userRepository.CreateEmailConfirmationToken(command.Id, cancellationToken);
        if (resultToken.IsFailed)
        {
            return resultToken.ToResult();
        }

        var uriBuilder = new UriBuilder(command.CallbackUrl);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);
        query["token"] = resultToken.Value;
        uriBuilder.Query = query.ToString();

        var callbackUrl = uriBuilder.ToString();

        var resultEmail = await _emailService.EnqueueConfirmRegistration(
            resultUser.Value.Email,
            resultUser.Value.Name.Split(' ')[0],
            callbackUrl,
            cancellationToken);

        return resultEmail;
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

        var resultVerify = await _userRepository.VerifyPassword(command.Id, command.Password, cancellationToken);
        if (resultVerify.IsFailed)
        {
            return resultVerify;
        }

        var resultToken = await _userRepository.CreateChangeEmailToken(command.Id, command.NewEmail, cancellationToken);
        if (resultToken.IsFailed)
        {
            return resultToken.ToResult();
        }

        var uriBuilder = new UriBuilder(command.CallbackUrl);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);
        query["token"] = resultToken.Value;
        uriBuilder.Query = query.ToString();

        var callbackUrl = uriBuilder.ToString();

        var resultEmail = await _emailService.EnqueueChangeEmail(
            command.NewEmail,
            resultUser.Value.Name.Split(' ')[0],
            callbackUrl,
            cancellationToken);

        return resultEmail;
    }
}
