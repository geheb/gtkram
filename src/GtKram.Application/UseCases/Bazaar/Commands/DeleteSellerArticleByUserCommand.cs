using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct DeleteSellerArticleByUserCommand(Guid UserId, Guid Id) : ICommand<Result>;