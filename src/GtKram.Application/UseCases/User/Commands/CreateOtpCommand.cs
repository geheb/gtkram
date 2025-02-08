using GtKram.Domain.Base;
using GtKram.Application.UseCases.User.Models;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public sealed record CreateOtpCommand(Guid Id) : ICommand<Result<UserOtp>>;