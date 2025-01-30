using FluentResults;
using GtKram.Domain.Models;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public sealed record UpdateBazaarEventCommand(BazaarEvent Event) : ICommand<Result>;

