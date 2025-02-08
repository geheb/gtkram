using GtKram.Domain.Base;
using GtKram.Application.UseCases.User.Models;
using Mediator;

namespace GtKram.Application.UseCases.User.Queries;

public sealed record GetOtpQuery(Guid Id) : IQuery<Result<UserOtp>>;