using GtKram.Domain.Base;
using GtKram.Application.UseCases.Bazaar.Models;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Queries;

public record struct FindSellerWithRegistrationAndArticlesQuery(Guid SellerId) : IQuery<Result<BazaarSellerWithRegistrationAndArticles>>;