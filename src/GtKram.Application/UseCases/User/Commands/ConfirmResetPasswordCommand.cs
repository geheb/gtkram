using ErrorOr;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public record struct ConfirmResetPasswordCommand(Guid Id, string NewPassword, string Token) : ICommand<ErrorOr<Success>>;
