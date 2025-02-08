using GtKram.Domain.Base;
using GtKram.Domain.Models;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Queries;

public sealed record FindEventQuery(Guid Id, bool ShouldValidate) : IQuery<Result<BazaarEvent>>;
