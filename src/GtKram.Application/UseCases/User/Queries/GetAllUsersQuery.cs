namespace GtKram.Application.UseCases.User.Queries;

using GtKram.Domain.Models;
using Mediator;

public record struct GetAllUsersQuery(UserRoleType? Role = null) : IQuery<Domain.Models.User[]>;
