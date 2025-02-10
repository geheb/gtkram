using GtKram.Domain.Base;
using GtKram.Application.UseCases.User.Models;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public record struct CreateOtpCommand(Guid Id) : ICommand<Result<UserOtp>>;