using ErrorOr;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct DeleteCheckoutArticleByUserCommand(Guid UserId, Guid CheckoutId, Guid ArticleId) : ICommand<ErrorOr<Success>>;
