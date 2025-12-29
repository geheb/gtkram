using ErrorOr;
using Mediator;

namespace GtKram.Application.UseCases.User.Queries;

public record struct FindUserByIdQuery(Guid Id) : IQuery<ErrorOr<Domain.Models.User>>;
