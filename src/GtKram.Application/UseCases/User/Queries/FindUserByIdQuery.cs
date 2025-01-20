using FluentResults;
using Mediator;

namespace GtKram.Application.UseCases.User.Queries;

public sealed record FindUserByIdQuery(Guid Id) : IQuery<Result<Domain.Models.User>>;
