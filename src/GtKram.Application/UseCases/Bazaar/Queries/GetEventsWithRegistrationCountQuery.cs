using GtKram.Domain.Models;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Queries;

public sealed record GetEventsWithRegistrationCountQuery : IQuery<BazaarEventWithRegistrationCount[]>;
