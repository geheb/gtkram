using FluentResults;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public sealed record AcceptSellerRegistrationCommand(Guid Id, string ConfirmUserCallbackUrl) : ICommand<Result>;
