using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public sealed record SignOutCommand(Guid Id) : ICommand<Result>;
