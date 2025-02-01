using FluentResults;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public sealed record SendAcceptSellerEmailCommand(Guid UserId, Guid BazaarEventId) : ICommand<Result>;
