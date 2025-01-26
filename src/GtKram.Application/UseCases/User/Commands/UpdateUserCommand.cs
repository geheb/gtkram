using FluentResults;
using GtKram.Domain.Models;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public sealed record UpdateUserCommand(Guid Id, string? Name, UserRoleType[]? Roles) : ICommand<Result>;
