using ErrorOr;
using GtKram.Application.UseCases.Bazaar.Models;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Queries;

public record struct FindSellerWithRegistrationAndArticlesQuery(Guid SellerRegistrationId) : IQuery<ErrorOr<SellerWithRegistrationAndArticles>>;