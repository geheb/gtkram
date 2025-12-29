using ErrorOr;
using GtKram.Domain.Models;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Queries;

public record struct FindEventByCheckoutQuery(Guid CheckoutId) : IQuery<ErrorOr<Event>>;