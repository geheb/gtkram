using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Domain.Models;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Queries;

public record GetSellerRegistrationWithArticleCountQuery(Guid EventId) : IQuery<BazaarSellerRegistrationWithArticleCount[]>;
