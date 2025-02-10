using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct TakeOverSellerArticlesCommand(Guid Id, Guid UserId) : ICommand<Result>;