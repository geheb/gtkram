using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public record struct BazaarEventWithBillingTotals(BazaarEvent Event, int BillingCount, decimal SoldTotal, decimal CommissionTotal);