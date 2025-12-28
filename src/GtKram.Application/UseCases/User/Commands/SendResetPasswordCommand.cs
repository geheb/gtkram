using ErrorOr;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public record struct SendResetPasswordCommand(string Email, string CallbackUrl) : ICommand<ErrorOr<Success>>;