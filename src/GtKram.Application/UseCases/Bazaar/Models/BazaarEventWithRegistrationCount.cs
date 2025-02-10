using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public record struct BazaarEventWithRegistrationCount(BazaarEvent Event, int RegistrationCount);