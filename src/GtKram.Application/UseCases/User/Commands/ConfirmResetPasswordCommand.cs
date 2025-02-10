using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public record struct ConfirmResetPasswordCommand(Guid Id, string NewPassword, string Token) : ICommand<Result>;
