using FluentResults;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public sealed record DeleteBazaarEventCommand(Guid Id) : ICommand<Result>;
