namespace GtKram.Application.UseCases.User.Queries;

using Mediator;

public record struct GetAllUsersQuery() : IQuery<Domain.Models.User[]>;
