namespace GtKram.Application.UseCases.User.Queries;

using Mediator;

public sealed record GetAllUsersQuery() : IQuery<Domain.Models.User[]>;
