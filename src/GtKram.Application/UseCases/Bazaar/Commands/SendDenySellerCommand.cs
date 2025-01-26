using FluentResults;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public sealed record SendDenySellerCommand(string Email, string Name, Guid BazaarEventId) : ICommand<Result>;
