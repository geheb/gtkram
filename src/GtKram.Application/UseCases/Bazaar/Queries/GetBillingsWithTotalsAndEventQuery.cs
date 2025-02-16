using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Queries;

public record struct GetBillingsWithTotalsAndEventQuery(Guid EventId) : IQuery<Result<BazaarBillingsWithTotalsAndEvent>>;