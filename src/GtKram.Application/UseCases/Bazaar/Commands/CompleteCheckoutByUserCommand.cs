using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct CompleteCheckoutByUserCommand(Guid UserId, Guid CheckoutId) : ICommand<Result>;