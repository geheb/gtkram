using FluentResults;
using Mediator;

namespace GtKram.Application.UseCases.User.Queries;

public sealed record VerifyConfirmRegistrationQuery(Guid Id, string Token) : IQuery<Result>;