using ErrorOr;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct CreateArticleByUserCommand(
    Guid UserId,
    Guid SellerId,
    string Name,
    string Size,
    decimal Price) : ICommand<ErrorOr<Success>>;
