using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public record struct ArticlesWithCheckoutAndEvent(Event Event, Checkout Checkout, ArticleWithCheckout[] Articles);