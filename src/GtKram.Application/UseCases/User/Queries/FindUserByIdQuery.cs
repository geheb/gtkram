using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.User.Queries;

public record struct FindUserByIdQuery(Guid Id) : IQuery<Result<Domain.Models.User>>;
