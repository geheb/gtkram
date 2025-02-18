using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct DeleteBillingArticleByUserCommand(Guid UserId, Guid BillingArticleId) : ICommand<Result>;
