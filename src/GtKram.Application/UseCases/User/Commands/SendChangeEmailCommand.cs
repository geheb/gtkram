using ErrorOr;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public record struct SendChangeEmailCommand(Guid Id, string NewEmail, string Password, string CallbackUrl) : ICommand<ErrorOr<Success>>;