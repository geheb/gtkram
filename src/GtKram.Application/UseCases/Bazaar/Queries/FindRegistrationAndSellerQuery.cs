using FluentResults;
using GtKram.Domain.Models;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Queries;

public sealed record FindRegistrationAndSellerQuery(Guid Id) : 
    IQuery<Result<(BazaarSellerRegistration Registration, BazaarSeller? Seller)>>;
