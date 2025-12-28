using ErrorOr;
using Mediator;

namespace GtKram.Application.UseCases.User.Queries;

public record struct VerifyConfirmRegistrationQuery(Guid Id, string Token) : IQuery<ErrorOr<Success>>;