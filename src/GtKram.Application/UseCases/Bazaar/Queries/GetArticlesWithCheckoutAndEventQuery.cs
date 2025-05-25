using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Queries;

public record struct GetArticlesWithCheckoutAndEventQuery(Guid CheckoutId) : IQuery<Result<ArticlesWithCheckoutAndEvent>>;
