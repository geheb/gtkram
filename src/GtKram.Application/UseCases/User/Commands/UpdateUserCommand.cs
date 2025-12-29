using ErrorOr;
using GtKram.Domain.Models;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public record struct UpdateUserCommand(Guid Id, string? Name, UserRoleType[]? Roles) : ICommand<ErrorOr<Success>>;
