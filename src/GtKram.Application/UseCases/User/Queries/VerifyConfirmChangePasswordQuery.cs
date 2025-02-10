using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.User.Queries;

public record struct VerifyConfirmChangePasswordQuery(Guid Id, string Token) : IQuery<Result>;
