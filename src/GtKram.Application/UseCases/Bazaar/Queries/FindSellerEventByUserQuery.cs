using GtKram.Domain.Base;
using GtKram.Domain.Models;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Queries;

public record struct FindSellerEventByUserQuery(Guid UserId, Guid BazaarSellerId) : IQuery<Result<BazaarEvent>>;