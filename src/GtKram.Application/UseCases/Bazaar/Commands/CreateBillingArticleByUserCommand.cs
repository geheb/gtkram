using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct CreateBillingArticleByUserCommand(
    Guid UserId, 
    Guid BillingId, 
    Guid SellerArticleId) : ICommand<Result<Guid>>;
