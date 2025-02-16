using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct CompleteBillingCommand(Guid Id) : ICommand<Result>;