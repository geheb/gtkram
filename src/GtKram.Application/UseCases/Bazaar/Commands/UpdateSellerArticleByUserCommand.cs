using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct UpdateSellerArticleByUserCommand(
    Guid UserId,
    Guid BazaarSellerArticleId,
    string Name,
    string Size,
    decimal Price) : ICommand<Result>;
