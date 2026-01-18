using ErrorOr;
using GtKram.Domain.Models;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct UpdatePlanningCommand(Planning Planning) : ICommand<ErrorOr<Success>>;