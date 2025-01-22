using FluentResults;
using GtKram.Application.UseCases.User.Models;
using Mediator;

namespace GtKram.Application.UseCases.User.Queries;

public sealed record GetTwoFactorAuthQuery(Guid Id) : IQuery<Result<UserTwoFactorAuthSettings>>;