using GtKram.Application.UseCases.Bazaar.Models;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Queries;

public record struct GetEventsWithBillingQuery(Guid UserId) : IQuery<BazaarEventWithBilling[]>;