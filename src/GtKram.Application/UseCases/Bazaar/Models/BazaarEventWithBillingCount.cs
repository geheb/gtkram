using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public record struct BazaarEventWithBillingCount(BazaarEvent Event, int BillingCount);