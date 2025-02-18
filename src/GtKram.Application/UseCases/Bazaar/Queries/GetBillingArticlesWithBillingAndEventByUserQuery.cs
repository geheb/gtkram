using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Queries;

public record struct GetBillingArticlesWithBillingAndEventByUserQuery(Guid UserId, Guid BillingId) : IQuery<Result<BazaarSellerArticlesWithBillingAndEvent>>;
