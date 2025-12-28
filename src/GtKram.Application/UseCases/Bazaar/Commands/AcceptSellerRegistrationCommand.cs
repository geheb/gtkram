using ErrorOr;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct AcceptSellerRegistrationCommand(Guid SellerRegistrationId, string ConfirmUserCallbackUrl) : ICommand<ErrorOr<Success>>;
