using FluentResults;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public sealed record ConfirmRegistrationCommand(Guid Id, string Password, string Token): ICommand<Result>;
