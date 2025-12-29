using ErrorOr;
using GtKram.Application.UseCases.User.Models;
using Mediator;

namespace GtKram.Application.UseCases.User.Queries;

public record struct GetOtpQuery(Guid Id) : IQuery<ErrorOr<UserOtp>>;