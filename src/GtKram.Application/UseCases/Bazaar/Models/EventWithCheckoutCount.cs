using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public record struct EventWithCheckoutCount(Event Event, int CheckoutCount);