using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Domain.Base;
using GtKram.Domain.Errors;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Handlers;

internal sealed class EventHandler :
    IQueryHandler<FindEventQuery, Result<Domain.Models.Event>>,
    IQueryHandler<FindEventForRegistrationQuery, Result<EventWithRegistrationCount>>,
    IQueryHandler<GetEventsWithRegistrationCountQuery, EventWithRegistrationCount[]>,
    ICommandHandler<CreateEventCommand, Result>,
    ICommandHandler<UpdateEventCommand, Result>,
    ICommandHandler<DeleteEventCommand, Result>
{
    private readonly IEventRepository _eventRepository;
    private readonly ISellerRegistrationRepository _sellerRegistrationRepository;

    public EventHandler(
        IEventRepository eventRepository,
        ISellerRegistrationRepository sellerRegistrationRepository)
    {
        _eventRepository = eventRepository;
        _sellerRegistrationRepository = sellerRegistrationRepository;
    }

    public async ValueTask<Result<Domain.Models.Event>> Handle(FindEventQuery query, CancellationToken cancellationToken) =>
        await _eventRepository.Find(query.EventId, cancellationToken);

    public async ValueTask<Result<EventWithRegistrationCount>> Handle(FindEventForRegistrationQuery query, CancellationToken cancellationToken)
    {
        var @event = await _eventRepository.Find(query.EventId, cancellationToken);
        if (@event.IsFailed)
        {
            return @event.ToResult();
        }

        var count = await _sellerRegistrationRepository.GetCountByEventId(query.EventId, cancellationToken);
        if (count.IsFailed)
        {
            return count.ToResult();
        }

        return Result.Ok(new EventWithRegistrationCount(@event.Value, count.Value));
    }

    public async ValueTask<EventWithRegistrationCount[]> Handle(GetEventsWithRegistrationCountQuery query, CancellationToken cancellationToken)
    {
        var events = await _eventRepository.GetAll(cancellationToken);
        if (events.Length == 0)
        {
            return [];
        }

        var registrations = await _sellerRegistrationRepository.GetAll(cancellationToken);

        var countByBazaarEventId = registrations
            .GroupBy(r => r.EventId)
            .ToDictionary(r => r.Key, r => r.Count());

        var results = events
            .Select(e => new EventWithRegistrationCount(e, countByBazaarEventId.TryGetValue(e.Id, out var count) ? count : 0))
            .ToArray();

        return results;
    }

    public async ValueTask<Result> Handle(CreateEventCommand command, CancellationToken cancellationToken)
    {
        var result = Validate(command.Event);

        return result.IsSuccess
            ? await _eventRepository.Create(command.Event, cancellationToken)
            : result;
    }

    public async ValueTask<Result> Handle(UpdateEventCommand command, CancellationToken cancellationToken)
    {
        var result = Validate(command.Event);

        return result.IsSuccess
            ? await _eventRepository.Update(command.Event, cancellationToken)
            : result;
    }

    public async ValueTask<Result> Handle(DeleteEventCommand command, CancellationToken cancellationToken)
    {
        var registrations = await _sellerRegistrationRepository.GetByEventId(command.EventId, cancellationToken);
        if (registrations.Length > 0)
        {
            return Result.Fail(Domain.Errors.Event.ValidationDeleteNotPossibleDueToRegistrations);
        }
        return await _eventRepository.Delete(command.EventId, cancellationToken);
    }

    private static Result Validate(Domain.Models.Event model)
    {
        if (model.Start >= model.End)
        {
            return Result.Fail(Domain.Errors.Event.ValidationDateFailed);
        }

        if (model.RegisterStart >= model.RegisterEnd || 
            model.RegisterStart > model.Start)
        {
            return Result.Fail(Domain.Errors.Event.ValidationRegisterDateFailed);
        }

        if (model.EditArticleEnd >= model.Start)
        {
            return Result.Fail(Domain.Errors.Event.ValidationEditArticleDateBeforeFailed);
        }
        else if (model.EditArticleEnd <= model.RegisterEnd)
        {
            return Result.Fail(Domain.Errors.Event.ValidationEditArticleDateAfterFailed);
        }

        if (model.PickUpLabelsStart >= model.PickUpLabelsEnd)
        {
            return Result.Fail(Domain.Errors.Event.ValidationPickUpLabelDateFailed);
        }
        else if (model.PickUpLabelsStart >= model.Start)
        {
            return Result.Fail(Domain.Errors.Event.ValidationPickupLabelDateBeforeFailed);
        }
        else if (model.PickUpLabelsStart <= model.EditArticleEnd)
        {
            return Result.Fail(Domain.Errors.Event.ValidationPickupLabelDateAfterFailed);
        }

        return Result.Ok();
    }
}
