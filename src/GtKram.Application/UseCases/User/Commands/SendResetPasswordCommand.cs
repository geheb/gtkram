using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public sealed record SendResetPasswordCommand(string Email, string CallbackUrl) : ICommand<Result>;