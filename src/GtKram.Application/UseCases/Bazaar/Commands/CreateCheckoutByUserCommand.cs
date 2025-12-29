using ErrorOr;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct CreateCheckoutByUserCommand(Guid UserId, Guid EventId) : ICommand<ErrorOr<Guid>>;