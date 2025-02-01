using FluentResults;
using GtKram.Domain.Models;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Queries;

public sealed record FindEventQuery(Guid Id) : IQuery<Result<BazaarEvent>>;
