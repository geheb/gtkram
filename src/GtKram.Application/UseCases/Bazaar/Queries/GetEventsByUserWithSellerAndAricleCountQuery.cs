using GtKram.Application.UseCases.Bazaar.Models;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Queries;

public record struct GetEventsByUserWithSellerAndAricleCountQuery(Guid Id) : IQuery<BazaarEventWithSellerAndArticleCount[]>;
