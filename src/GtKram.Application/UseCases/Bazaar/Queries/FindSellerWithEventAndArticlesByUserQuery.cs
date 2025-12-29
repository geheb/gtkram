using ErrorOr;
using GtKram.Application.UseCases.Bazaar.Models;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Queries;

public record struct FindSellerWithEventAndArticlesByUserQuery(Guid UserId, Guid SellerId) : IQuery<ErrorOr<SellerWithEventAndArticles>>;