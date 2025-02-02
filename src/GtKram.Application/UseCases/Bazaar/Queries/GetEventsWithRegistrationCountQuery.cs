using GtKram.Application.UseCases.Bazaar.Models;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Queries;

public sealed record GetEventsWithRegistrationCountQuery : IQuery<BazaarEventWithRegistrationCount[]>;
