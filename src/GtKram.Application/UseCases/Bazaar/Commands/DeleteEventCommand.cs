using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public sealed record DeleteEventCommand(Guid Id) : ICommand<Result>;
