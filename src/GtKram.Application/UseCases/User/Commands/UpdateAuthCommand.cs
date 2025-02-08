using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public sealed record UpdateAuthCommand(Guid Id, string? Email, string? Password) : ICommand<Result>;

