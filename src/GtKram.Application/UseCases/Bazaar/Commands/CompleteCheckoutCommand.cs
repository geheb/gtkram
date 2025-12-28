using ErrorOr;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct CompleteCheckoutCommand(Guid CheckoutId) : ICommand<ErrorOr<Success>>;