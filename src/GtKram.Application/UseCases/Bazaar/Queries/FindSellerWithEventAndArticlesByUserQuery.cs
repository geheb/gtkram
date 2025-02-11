using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Queries;

public record struct FindSellerWithEventAndArticlesByUserQuery(Guid UserId, Guid Id) : IQuery<Result<BazaarSellerWithEventAndArticles>>;