using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public record struct EventWithPlanningCount(Event Event, int PlanningCount);
