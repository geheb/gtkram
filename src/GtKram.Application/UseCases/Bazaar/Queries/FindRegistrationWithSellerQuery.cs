using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Queries;

public sealed record FindRegistrationWithSellerQuery(Guid Id) : IQuery<Result<BazaarSellerRegistrationWithSeller>>;
