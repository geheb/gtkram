using ErrorOr;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct CancelCheckoutCommand(Guid CheckoutId) : ICommand<ErrorOr<Success>>;
