using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Queries;

public record struct GetBillingArticlesWithBillingAndEventQuery(Guid BillingId) : IQuery<Result<BazaarSellerArticlesWithBillingAndEvent>>;
