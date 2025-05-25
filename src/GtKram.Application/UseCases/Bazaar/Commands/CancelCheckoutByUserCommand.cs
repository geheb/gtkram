using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct CancelCheckoutByUserCommand(Guid UserId, Guid CheckoutId) : ICommand<Result>;
