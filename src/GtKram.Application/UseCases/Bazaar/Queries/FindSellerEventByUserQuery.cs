using ErrorOr;
using GtKram.Domain.Models;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Queries;

public record struct FindSellerEventByUserQuery(Guid UserId, Guid SellerId) : IQuery<ErrorOr<Event>>;