using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public record struct CheckoutWithTotals(Checkout Checkout, string CreatedBy, int ArticleCount, decimal Total);