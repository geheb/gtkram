using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct DeleteArticleByUserCommand(Guid UserId, Guid ArticleId) : ICommand<Result>;