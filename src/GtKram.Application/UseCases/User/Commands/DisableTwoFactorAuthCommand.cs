using FluentResults;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public sealed record DisableTwoFactorAuthCommand(Guid Id, string Code) : ICommand<Result>;
