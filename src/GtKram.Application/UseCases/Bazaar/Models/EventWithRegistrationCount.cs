using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public record struct EventWithRegistrationCount(Event Event, int RegistrationCount);