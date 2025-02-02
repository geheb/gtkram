using FluentResults;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public sealed record DeleteSellerRegistrationCommand(Guid Id) : ICommand<Result>;

