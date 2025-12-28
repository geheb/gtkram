namespace GtKram.Application.UseCases.User.Commands;

using ErrorOr;
using Mediator;

public record struct ConfirmChangeEmailCommand(Guid Id, string NewEmail, string Token) : ICommand<ErrorOr<Success>>;