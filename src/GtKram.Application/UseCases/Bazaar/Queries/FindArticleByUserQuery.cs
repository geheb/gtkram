using ErrorOr;
using GtKram.Application.UseCases.Bazaar.Models;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Queries;

public record struct FindArticleByUserQuery(Guid UserId, Guid ArticleId) : IQuery<ErrorOr<ArticleWithEvent>>;