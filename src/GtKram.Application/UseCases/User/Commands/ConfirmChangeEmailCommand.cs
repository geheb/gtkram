namespace GtKram.Application.UseCases.User.Commands;

using FluentResults;
using Mediator;

public sealed record class ConfirmChangeEmailCommand(Guid Id, string NewEmail, string Token) : ICommand<Result>;