using GtKram.Domain.Base;
using GtKram.Domain.Models;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public record struct CreateUserCommand(string Name, string Email, UserRoleType[] Roles, string CallbackUrl) : ICommand<Result<Guid>>;