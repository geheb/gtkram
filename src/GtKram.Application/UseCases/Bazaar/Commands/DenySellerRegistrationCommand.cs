using FluentResults;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public sealed record DenySellerRegistrationCommand(Guid Id) : ICommand<Result>;
