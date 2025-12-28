using ErrorOr;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct CompleteCheckoutByUserCommand(Guid UserId, Guid CheckoutId) : ICommand<ErrorOr<Success>>;