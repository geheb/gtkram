using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct CreateCheckoutByUserCommand(Guid UserId, Guid EventId) : ICommand<Result<Guid>>;