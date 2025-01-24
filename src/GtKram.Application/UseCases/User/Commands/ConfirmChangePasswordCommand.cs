using FluentResults;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public sealed record ConfirmChangePasswordCommand(Guid Id, string NewPassword, string Token) : ICommand<Result>;
