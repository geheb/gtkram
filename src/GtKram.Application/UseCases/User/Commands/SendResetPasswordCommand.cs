using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public record struct SendResetPasswordCommand(string Email, string CallbackUrl) : ICommand<Result>;