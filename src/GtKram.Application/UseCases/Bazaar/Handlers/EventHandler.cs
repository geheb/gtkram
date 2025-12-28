using ErrorOr;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Domain.Repositories;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Handlers;

internal sealed class EventHandler :
    IQueryHandler<FindEventQuery, ErrorOr<Domain.Models.Event>>,
    IQueryHandler<FindEventForRegistrationQuery, ErrorOr<EventWithRegistrationCount>>,
    IQueryHandler<GetEventsWithRegistrationCountQuery, EventWithRegistrationCount[]>,
    ICommandHandler<CreateEventCommand, ErrorOr<Success>>,
    ICommandHandler<UpdateEventCommand, ErrorOr<Success>>,
    ICommandHandler<DeleteEventCommand, ErrorOr<Success>>
{
    private readonly IEvents _events;
    private readonly ISellerRegistrations _sellerRegistrations;

    public EventHandler(
        IEvents events,
        ISellerRegistrations sellerRegistrations)
    {
        _events = events;
        _sellerRegistrations = sellerRegistrations;
    }

    public async ValueTask<ErrorOr<Domain.Models.Event>> Handle(FindEventQuery query, CancellationToken cancellationToken) =>
        await _events.Find(query.EventId, cancellationToken);

    public async ValueTask<ErrorOr<EventWithRegistrationCount>> Handle(FindEventForRegistrationQuery query, CancellationToken cancellationToken)
    {
        var @event = await _events.Find(query.EventId, cancellationToken);
        if (@event.IsError)
        {
            return @event.Errors;
        }

        var count = await _sellerRegistrations.GetCountByEventId(query.EventId, cancellationToken);
        if (count.IsError)
        {
            return count.Errors;
        }

        return new EventWithRegistrationCount(@event.Value, count.Value);
    }

    public async ValueTask<EventWithRegistrationCount[]> Handle(GetEventsWithRegistrationCountQuery query, CancellationToken cancellationToken)
    {
        var events = await _events.GetAll(cancellationToken);
        if (events.Length == 0)
        {
            return [];
        }

        var registrations = await _sellerRegistrations.GetAll(cancellationToken);

        var countByBazaarEventId = registrations
            .GroupBy(r => r.EventId)
            .ToDictionary(r => r.Key, r => r.Count());

        var results = events
            .Select(e => new EventWithRegistrationCount(e, countByBazaarEventId.TryGetValue(e.Id, out var count) ? count : 0))
            .ToArray();

        return results;
    }

    public async ValueTask<ErrorOr<Success>> Handle(CreateEventCommand command, CancellationToken cancellationToken)
    {
        var result = Validate(command.Event);

        if (result.IsError)
        {
            return result;
        }

        var idResult = await _events.Create(command.Event, cancellationToken);

        return idResult.IsError ? idResult.Errors : Result.Success;
    }

    public async ValueTask<ErrorOr<Success>> Handle(UpdateEventCommand command, CancellationToken cancellationToken)
    {
        var result = Validate(command.Event);

        return !result.IsError
            ? await _events.Update(command.Event, cancellationToken)
            : result;
    }

    public async ValueTask<ErrorOr<Success>> Handle(DeleteEventCommand command, CancellationToken cancellationToken)
    {
        var registrations = await _sellerRegistrations.GetByEventId(command.EventId, cancellationToken);
        if (registrations.Length > 0)
        {
            return Domain.Errors.Event.ValidationDeleteNotPossibleDueToRegistrations;
        }
        return await _events.Delete(command.EventId, cancellationToken);
    }

    private static ErrorOr<Success> Validate(Domain.Models.Event model)
    {
        if (model.Start >= model.End)
        {
            return Domain.Errors.Event.ValidationDateFailed;
        }

        if (model.RegisterStart >= model.RegisterEnd || 
            model.RegisterStart > model.Start)
        {
            return Domain.Errors.Event.ValidationRegisterDateFailed;
        }

        if (model.EditArticleEnd >= model.Start)
        {
            return Domain.Errors.Event.ValidationEditArticleDateBeforeFailed;
        }
        else if (model.EditArticleEnd <= model.RegisterEnd)
        {
            return Domain.Errors.Event.ValidationEditArticleDateAfterFailed;
        }

        if (model.PickUpLabelsStart >= model.PickUpLabelsEnd)
        {
            return Domain.Errors.Event.ValidationPickUpLabelDateFailed;
        }
        else if (model.PickUpLabelsStart >= model.Start)
        {
            return Domain.Errors.Event.ValidationPickupLabelDateBeforeFailed;
        }
        else if (model.PickUpLabelsStart <= model.EditArticleEnd)
        {
            return Domain.Errors.Event.ValidationPickupLabelDateAfterFailed;
        }

        return Result.Success;
    }
}
