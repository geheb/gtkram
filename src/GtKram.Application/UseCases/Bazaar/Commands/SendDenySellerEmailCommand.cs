using FluentResults;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public sealed record SendDenySellerEmailCommand(string Email, string Name, Guid BazaarEventId) : ICommand<Result>;
