using FluentResults;
using GtKram.Application.Services;
using GtKram.Application.UseCases.User.Commands;
using GtKram.Domain.Repositories;
using Mediator;
using System.Web;

namespace GtKram.Application.UseCases.User.Handler;

internal sealed class EnqueueEmailHandler :
    ICommandHandler<SendConfirmRegistrationCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;

    public EnqueueEmailHandler(
        IUserRepository userRepository,
        IEmailService emailService)
    {
        _userRepository = userRepository;
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
}
