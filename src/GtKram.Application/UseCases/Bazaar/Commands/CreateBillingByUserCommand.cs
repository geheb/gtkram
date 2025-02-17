using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct CreateBillingByUserCommand(Guid UserId, Guid EventId) : ICommand<Result<Guid>>;