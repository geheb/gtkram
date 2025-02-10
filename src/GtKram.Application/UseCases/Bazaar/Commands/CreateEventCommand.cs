using GtKram.Domain.Base;
using GtKram.Domain.Models;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct CreateEventCommand(BazaarEvent Event) : ICommand<Result>;