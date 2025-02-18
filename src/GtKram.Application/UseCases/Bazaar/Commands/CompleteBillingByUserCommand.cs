using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct CompleteBillingByUserCommand(Guid UserId, Guid BillingId) : ICommand<Result>;