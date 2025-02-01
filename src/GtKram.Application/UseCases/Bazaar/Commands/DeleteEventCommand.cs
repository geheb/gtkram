using FluentResults;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public sealed record DeleteEventCommand(Guid Id) : ICommand<Result>;
