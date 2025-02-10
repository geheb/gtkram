namespace GtKram.Application.UseCases.User.Commands;

using GtKram.Domain.Base;
using Mediator;

public record struct ConfirmChangeEmailCommand(Guid Id, string NewEmail, string Token) : ICommand<Result>;