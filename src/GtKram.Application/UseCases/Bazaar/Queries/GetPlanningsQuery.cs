using GtKram.Domain.Models;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Queries;

public record struct GetPlanningsQuery(Guid EventId) : IQuery<Planning[]>;
