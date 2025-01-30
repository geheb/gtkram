namespace GtKram.Domain.Models;

public sealed record BazaarEventWithRegistrationCount(BazaarEvent Event, int RegistrationCount);