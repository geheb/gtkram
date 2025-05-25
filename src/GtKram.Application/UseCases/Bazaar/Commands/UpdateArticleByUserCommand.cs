using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct UpdateArticleByUserCommand(
    Guid UserId,
    Guid ArticleId,
    string Name,
    string Size,
    decimal Price) : ICommand<Result>;
