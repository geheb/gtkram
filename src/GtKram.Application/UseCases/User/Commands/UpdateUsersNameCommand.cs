using FluentResults;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public sealed record UpdateUsersNameCommand(Guid Id, string Name) : ICommand<Result>;
