using FluentResults;
using Mediator;

namespace GtKram.Application.UseCases.User.Queries;

public sealed record VerifyConfirmChangePasswordQuery(Guid Id, string Token) : IQuery<Result>;
