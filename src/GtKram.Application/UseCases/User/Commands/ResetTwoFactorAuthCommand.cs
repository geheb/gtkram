using FluentResults;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public sealed record ResetTwoFactorAuthCommand(Guid Id) : ICommand<Result>;
