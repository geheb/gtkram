using FluentResults;
using GtKram.Domain.Models;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public sealed record UpdateUserCommand(Guid Id, string? Name, string? Email, string? Password, UserRoleType[]? Roles) : ICommand<Result>;
