using GtKram.Domain.Base;
using GtKram.Domain.Models;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public sealed record CreateEventCommand(BazaarEvent Event) : ICommand<Result>;