using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct DeleteCheckoutArticleCommand(Guid CheckoutId, Guid ArticleId) : ICommand<Result>;
