using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public record struct CheckoutWithTotalsAndEvent(CheckoutWithTotals[] Checkouts, Event Event);