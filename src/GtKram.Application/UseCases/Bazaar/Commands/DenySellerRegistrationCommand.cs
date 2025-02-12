using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct DenySellerRegistrationCommand(Guid BazaarSellerRegistrationId) : ICommand<Result>;
