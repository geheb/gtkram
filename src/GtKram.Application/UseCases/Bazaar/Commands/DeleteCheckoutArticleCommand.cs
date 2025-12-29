using ErrorOr;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct DeleteCheckoutArticleCommand(Guid CheckoutId, Guid ArticleId) : ICommand<ErrorOr<Success>>;
