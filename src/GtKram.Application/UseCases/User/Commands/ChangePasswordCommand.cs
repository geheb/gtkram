using FluentResults;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public sealed record ChangePasswordCommand(Guid Id, string CurrentPassword, string NewPassword) : ICommand<Result>;