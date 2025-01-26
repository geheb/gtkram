using FluentResults;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public sealed record ConfirmResetPasswordCommand(Guid Id, string NewPassword, string Token) : ICommand<Result>;
