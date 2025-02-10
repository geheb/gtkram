using GtKram.Domain.Base;
using GtKram.Application.UseCases.User.Models;
using Mediator;

namespace GtKram.Application.UseCases.User.Queries;

public record struct GetOtpQuery(Guid Id) : IQuery<Result<UserOtp>>;