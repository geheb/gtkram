using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct TakeOverSellerArticlesByUserCommand(Guid UserId, Guid SellerId) : ICommand<Result>;