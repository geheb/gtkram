using FluentResults;
using GtKram.Application.Converter;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Handlers;

internal sealed class EventHandler :
    IQueryHandler<FindEventQuery, Result<BazaarEvent>>,
    IQueryHandler<GetEventsWithRegistrationCountQuery, BazaarEventWithRegistrationCount[]>,
    ICommandHandler<CreateEventCommand, Result>,
    ICommandHandler<UpdateEventCommand, Result>,
    ICommandHandler<DeleteEventCommand, Result>
{
    private readonly IBazaarEventRepository _eventRepository;
    private readonly IBazaarSellerRegistrationRepository _sellerRegistrationRepository;

    public EventHandler(
        IBazaarEventRepository eventRepository,
        IBazaarSellerRegistrationRepository sellerRegistrationRepository)
    {
        _eventRepository = eventRepository;
        _sellerRegistrationRepository = sellerRegistrationRepository;
    }

    public async ValueTask<Result<BazaarEvent>> Handle(FindEventQuery query, CancellationToken cancellationToken) =>
        await _eventRepository.Find(query.Id, cancellationToken);

    public async ValueTask<BazaarEventWithRegistrationCount[]> Handle(GetEventsWithRegistrationCountQuery query, CancellationToken cancellationToken)
    {
        var events = await _eventRepository.GetAll(cancellationToken);
        if (events.Length == 0)
        {
            return [];
        }

        var countByBazaarEventId = await _sellerRegistrationRepository.GetCountByBazaarEventId(cancellationToken);

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
        var registrations = await _sellerRegistrationRepository.GetByBazaarEventId(command.Id, cancellationToken);
        if (registrations.Length > 0)
        {
            return Result.Fail("Der Kinderbasar kann nicht gelöscht werden, da Registrierungen vorliegen.");
        }
        return await _eventRepository.Delete(command.Id, cancellationToken);
    }

    private static Result Validate(BazaarEvent model)
    {
        if (model.StartsOn >= model.EndsOn)
        {
            return Result.Fail("Das Datum für den Kinderbasar ist ungültig.");
        }

        if (model.RegisterStartsOn >= model.RegisterEndsOn || 
            model.RegisterStartsOn > model.StartsOn)
        {
            return Result.Fail("Die Registrierung der Verkäufer sollte vor dem Datum des Kinderbasars stattfinden.");
        }

        if (model.EditArticleEndsOn >= model.StartsOn)
        {
            return Result.Fail("Die Bearbeitung der Artikel sollte vor dem Datum des Kinderbasars stattfinden.");
        }
        else if (model.EditArticleEndsOn <= model.RegisterEndsOn)
        {
            return Result.Fail("Die Bearbeitung der Artikel sollte nach dem Datum für die Registrierung liegen.");
        }

        if (model.PickUpLabelsStartsOn >= model.PickUpLabelsEndsOn)
        {
            return Result.Fail("Das Datum für die Abholung der Etiketten ist ungültig.");
        }
        else if (model.PickUpLabelsStartsOn >= model.StartsOn)
        {
            return Result.Fail("Die Abholung der Etiketten sollte vor dem Kinderbasar stattfinden.");
        }
        else if (model.PickUpLabelsStartsOn <= model.EditArticleEndsOn)
        {
            return Result.Fail("Die Abholung der Etiketten sollte nach dem Datum für die Bearbeitung der Artikel liegen.");
        }

        return Result.Ok();
    }
}
