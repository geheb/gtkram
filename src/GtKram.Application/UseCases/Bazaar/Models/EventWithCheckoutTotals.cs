using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public record struct EventWithCheckoutTotals(Event Event, int CheckoutCount, decimal SoldTotal, decimal CommissionTotal);