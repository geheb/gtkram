using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public sealed record BazaarEventWithRegistrationCount(BazaarEvent Event, int RegistrationCount);