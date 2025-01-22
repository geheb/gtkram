using FluentResults;
using GtKram.Domain.Models;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public sealed record CreateUserCommand(string Name, string Email, UserRoleType[] Roles) : ICommand<Result>;