using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Queries;

public record struct GetCheckoutWithTotalsAndEventByUserQuery(Guid UserId, Guid EventId) : IQuery<Result<CheckoutWithTotalsAndEvent>>;