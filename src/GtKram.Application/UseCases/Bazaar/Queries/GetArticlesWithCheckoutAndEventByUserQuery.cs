using ErrorOr;
using GtKram.Application.UseCases.Bazaar.Models;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Queries;

public record struct GetArticlesWithCheckoutAndEventByUserQuery(Guid UserId, Guid CheckoutId) : IQuery<ErrorOr<ArticlesWithCheckoutAndEvent>>;
