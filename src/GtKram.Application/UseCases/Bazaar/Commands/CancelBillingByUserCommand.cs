using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct CancelBillingByUserCommand(Guid UserId, Guid BillingId) : ICommand<Result>;
