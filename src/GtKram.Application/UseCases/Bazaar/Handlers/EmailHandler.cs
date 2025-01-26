using FluentResults;
using GtKram.Application.Services;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Domain.Repositories;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Handlers;

internal sealed class EmailHandler :
    ICommandHandler<SendAcceptSellerCommand, Result>,
    ICommandHandler<SendDenySellerCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IBazaarEventRepository _bazaarEventRepository;
    private readonly IEmailService _emailService;

    public EmailHandler(
        IUserRepository userRepository,
        IBazaarEventRepository bazaarEventRepository,
        IEmailService emailService)
    {
        _userRepository = userRepository;
        _bazaarEventRepository = bazaarEventRepository;
        _emailService = emailService;
    }

    public async ValueTask<Result> Handle(SendAcceptSellerCommand command, CancellationToken cancellationToken)
    {
        var resultUser = await _userRepository.Find(command.UserId, cancellationToken);
        if (resultUser.IsFailed)
        {
            return resultUser.ToResult();
        }

        var resultEvent = await _bazaarEventRepository.Find(command.BazaarEventId, cancellationToken);
        if (resultEvent.IsFailed)
        {
            return resultEvent.ToResult();
        }

        var result = await _emailService.EnqueueAcceptSeller(
            resultEvent.Value,
            resultUser.Value,
            cancellationToken);

        return result;
    }

    public async ValueTask<Result> Handle(SendDenySellerCommand command, CancellationToken cancellationToken)
    {
        var resultEvent = await _bazaarEventRepository.Find(command.BazaarEventId, cancellationToken);
        if (resultEvent.IsFailed)
        {
            return resultEvent.ToResult();
        }

        var result = await _emailService.EnqueueDenySeller(
            resultEvent.Value,
            command.Email,
            command.Name,
            cancellationToken);

        return result;
    }
}
