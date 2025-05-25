using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct CreateCheckoutArticleByUserCommand(
    Guid UserId, 
    Guid CheckoutId, 
    Guid SellerArticleId) : ICommand<Result>;
