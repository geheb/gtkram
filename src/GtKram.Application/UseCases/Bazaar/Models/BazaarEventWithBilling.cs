using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public record struct BazaarEventWithBilling(BazaarEvent Event, int BillingCount, decimal SoldTotal, decimal CommissionTotal);