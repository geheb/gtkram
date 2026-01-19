using ErrorOr;
using GtKram.Application.Converter;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Handlers;

internal class PlanningHandler :
    ICommandHandler<CreatePlanningCommand, ErrorOr<Success>>,
    ICommandHandler<UpdatePlanningCommand, ErrorOr<Success>>,
    IQueryHandler<FindPlanningQuery, ErrorOr<Planning>>,
    IQueryHandler<GetPlanningsQuery, Planning[]>
{
    private readonly TimeProvider _timeProvider;
    private readonly IEvents _events;
    private readonly IPlannings _plannings;

    public PlanningHandler(
        TimeProvider timeProvider,
        IEvents events,
        IPlannings plannings)
    {
        _timeProvider = timeProvider;
        _events = events;
        _plannings = plannings;
    }

    public async ValueTask<ErrorOr<Success>> Handle(CreatePlanningCommand command, CancellationToken cancellationToken)
    {
        var @event = await _events.Find(command.Planning.EventId, cancellationToken);
        if (@event.IsError)
        {
            return @event.Errors;
        }

        var converter = new EventConverter();
        if (converter.IsExpired(@event.Value, _timeProvider))
        {
            return Domain.Errors.Event.Expired;
        }

        var errorOrSuccess = Validate(command.Planning);
        if (errorOrSuccess.IsError)
        {
            return errorOrSuccess;
        }

        return await _plannings.Create(command.Planning, cancellationToken);
    }

    public async ValueTask<Planning[]> Handle(GetPlanningsQuery query, CancellationToken cancellationToken)
    {
        var plannings = await _plannings.GetByEventId(query.EventId, cancellationToken);
        return [.. plannings.OrderBy(p => p.Name).ThenBy(p => p.Date).ThenBy(p => p.From)];
    }

    public async ValueTask<ErrorOr<Success>> Handle(UpdatePlanningCommand command, CancellationToken cancellationToken)
    {
        return await _plannings.Update(command.Planning, cancellationToken);
    }

    public async ValueTask<ErrorOr<Planning>> Handle(FindPlanningQuery query, CancellationToken cancellationToken)
    {
        return await _plannings.Find(query.Id, cancellationToken);
    }

    private static ErrorOr<Success> Validate(Domain.Models.Planning model)
    {
        if (model.Date == DateTimeOffset.MinValue)
        {
            return Domain.Errors.Planning.ValidationDateFailed;
        }

        if (model.From >= model.To)
        {
            return Domain.Errors.Planning.ValidationFromBeforeToFailed;
        }

        return Result.Success;
    }
}
