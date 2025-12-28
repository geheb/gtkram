using ErrorOr;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct DeleteEventCommand(Guid EventId) : ICommand<ErrorOr<Success>>;
