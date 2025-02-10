using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public record struct UpdateAuthCommand(Guid Id, string? Email, string? Password) : ICommand<Result>;

