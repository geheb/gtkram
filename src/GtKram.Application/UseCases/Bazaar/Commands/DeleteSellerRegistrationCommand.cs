using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct DeleteSellerRegistrationCommand(Guid BazaarSellerRegistrationId) : ICommand<Result>;

