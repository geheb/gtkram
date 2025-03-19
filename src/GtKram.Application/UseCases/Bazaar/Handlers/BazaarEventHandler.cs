using GtKram.Application.Converter;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Domain.Base;
using GtKram.Domain.Errors;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Handlers;

internal sealed class BazaarEventHandler :
    IQueryHandler<FindEventQuery, Result<BazaarEvent>>,
    IQueryHandler<FindEventForRegistrationQuery, Result<BazaarEventWithRegistrationCount>>,
    IQueryHandler<GetEventsWithRegistrationCountQuery, BazaarEventWithRegistrationCount[]>,
    ICommandHandler<CreateEventCommand, Result>,
    ICommandHandler<UpdateEventCommand, Result>,
    ICommandHandler<DeleteEventCommand, Result>
{
    private readonly TimeProvider _timeProvider;
    private readonly IBazaarEventRepository _eventRepository;
    private readonly IBazaarSellerRegistrationRepository _sellerRegistrationRepository;

    public BazaarEventHandler(
        TimeProvider timeProvider,
        IBazaarEventRepository eventRepository,
        IBazaarSellerRegistrationRepository sellerRegistrationRepository)
    {
        _timeProvider = timeProvider;
        _eventRepository = eventRepository;
        _sellerRegistrationRepository = sellerRegistrationRepository;
    }

    public async ValueTask<Result<BazaarEvent>> Handle(FindEventQuery query, CancellationToken cancellationToken) =>
        await _eventRepository.Find(query.EventId, cancellationToken);

    public async ValueTask<Result<BazaarEventWithRegistrationCount>> Handle(FindEventForRegistrationQuery query, CancellationToken cancellationToken)
    {
        var @event = await _eventRepository.Find(query.EventId, cancellationToken);
        if (@event.IsFailed)
        {
            return @event.ToResult();
        }

        var count = await _sellerRegistrationRepository.GetCountByBazaarEventId(query.EventId, cancellationToken);
        if (count.IsFailed)
        {
            return count.ToResult();
        }

        return Result.Ok(new BazaarEventWithRegistrationCount(@event.Value, count.Value));
    }

    public async ValueTask<BazaarEventWithRegistrationCount[]> Handle(GetEventsWithRegistrationCountQuery query, CancellationToken cancellationToken)
    {
        var events = await _eventRepository.GetAll(cancellationToken);
        if (events.Length == 0)
        {
            return [];
        }

        var registrations = await _sellerRegistrationRepository.GetAll(cancellationToken);

        var countByBazaarEventId = registrations
            .GroupBy(r => r.BazaarEventId)
            .ToDictionary(r => r.Key, r => r.Count());

        var results = events
            .Select(e => new BazaarEventWithRegistrationCount(e, countByBazaarEventId.TryGetValue(e.Id, out var count) ? count : 0))
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
        var registrations = await _sellerRegistrationRepository.GetByBazaarEventId(command.EventId, cancellationToken);
        if (registrations.Length > 0)
        {
            return Result.Fail(Event.ValidationDeleteNotPossibleDueToRegistrations);
        }
        return await _eventRepository.Delete(command.EventId, cancellationToken);
    }

    private static Result Validate(BazaarEvent model)
    {
        if (model.StartsOn >= model.EndsOn)
        {
            return Result.Fail(Event.ValidationDateFailed);
        }

        if (model.RegisterStartsOn >= model.RegisterEndsOn || 
            model.RegisterStartsOn > model.StartsOn)
        {
            return Result.Fail(Event.ValidationRegisterDateFailed);
        }

        if (model.EditArticleEndsOn >= model.StartsOn)
        {
            return Result.Fail(Event.ValidationEditArticleDateBeforeFailed);
        }
        else if (model.EditArticleEndsOn <= model.RegisterEndsOn)
        {
            return Result.Fail(Event.ValidationEditArticleDateAfterFailed);
        }

        if (model.PickUpLabelsStartsOn >= model.PickUpLabelsEndsOn)
        {
            return Result.Fail(Event.ValidationPickUpLabelDateFailed);
        }
        else if (model.PickUpLabelsStartsOn >= model.StartsOn)
        {
            return Result.Fail(Event.ValidationPickupLabelDateBeforeFailed);
        }
        else if (model.PickUpLabelsStartsOn <= model.EditArticleEndsOn)
        {
            return Result.Fail(Event.ValidationPickupLabelDateAfterFailed);
        }

        return Result.Ok();
    }
}
