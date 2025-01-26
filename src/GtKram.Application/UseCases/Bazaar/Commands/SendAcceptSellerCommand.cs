using FluentResults;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public sealed record SendAcceptSellerCommand(Guid UserId, Guid BazaarEventId) : ICommand<Result>;
