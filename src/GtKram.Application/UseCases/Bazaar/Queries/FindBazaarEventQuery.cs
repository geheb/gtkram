using FluentResults;
using GtKram.Domain.Models;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Queries;

public sealed record FindBazaarEventQuery(Guid Id) : IQuery<Result<BazaarEvent>>;
