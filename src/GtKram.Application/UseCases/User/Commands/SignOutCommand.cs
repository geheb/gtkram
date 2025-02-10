using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.User.Commands;

public record struct SignOutCommand(Guid Id) : ICommand<Result>;
