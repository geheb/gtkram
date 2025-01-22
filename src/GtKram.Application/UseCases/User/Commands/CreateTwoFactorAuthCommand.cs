using FluentResults;
using GtKram.Application.UseCases.User.Models;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public sealed record CreateTwoFactorAuthCommand(Guid Id) : ICommand<Result<UserTwoFactorAuthSettings>>;