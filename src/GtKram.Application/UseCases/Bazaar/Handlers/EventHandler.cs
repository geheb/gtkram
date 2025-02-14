using GtKram.Application.Converter;
using GtKram.Application.Repositories;
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
    IQueryHandler<FindEventQuery, Result<BazaarEvent>>,
    IQueryHandler<GetEventsWithRegistrationCountQuery, BazaarEventWithRegistrationCount[]>,
    IQueryHandler<GetEventsWithBillingQuery, BazaarEventWithBilling[]>,
    ICommandHandler<CreateEventCommand, Result>,
    ICommandHandler<UpdateEventCommand, Result>,
    ICommandHandler<DeleteEventCommand, Result>
{
    private readonly TimeProvider _timeProvider;
    private readonly IBazaarEventRepository _eventRepository;
    private readonly IBazaarSellerRepository _sellerRepository;
    private readonly IBazaarBillingRepository _billingRepository;
    private readonly IBazaarSellerRegistrationRepository _sellerRegistrationRepository;
    private readonly IBazaarBillingArticleRepository _billingArticleRepository;
    private readonly IBazaarSellerArticleRepository _sellerArticleRepository;

    public EventHandler(
        TimeProvider timeProvider,
        IBazaarEventRepository eventRepository,
        IBazaarSellerRepository sellerRepository,
        IBazaarBillingRepository billingRepository,
        IBazaarSellerRegistrationRepository sellerRegistrationRepository,
        IBazaarBillingArticleRepository billingArticleRepository,
        IBazaarSellerArticleRepository sellerArticleRepository)
    {
        _timeProvider = timeProvider;
        _eventRepository = eventRepository;
        _sellerRepository = sellerRepository;
        _billingRepository = billingRepository;
        _sellerRegistrationRepository = sellerRegistrationRepository;
        _billingArticleRepository = billingArticleRepository;
        _sellerArticleRepository = sellerArticleRepository;
    }

    public async ValueTask<Result<BazaarEvent>> Handle(FindEventQuery query, CancellationToken cancellationToken)
    {
        var @event = await _eventRepository.Find(query.BazaarEventId, cancellationToken);
        if (@event.IsFailed)
        {
            return @event;
        }

        if (!query.ShouldValidate)
        {
            return @event;
        }

        var converter = new EventConverter();
        if (converter.IsExpired(@event.Value, _timeProvider))
        {
            return Result.Fail(Event.Expired);
        }

        if (!converter.CanRegister(@event.Value, _timeProvider))
        {
            return Result.Fail(EventRegistration.NotReady);
        }

        var count = await _sellerRegistrationRepository.GetCountByBazaarEventId(@event.Value.Id, cancellationToken);
        if (count.IsFailed)
        {
            return count.ToResult();
        }

        if (count.Value >= @event.Value.MaxSellers)
        {
            return Result.Fail(EventRegistration.LimitExceeded);
        }
        
        return @event;
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
        var registrations = await _sellerRegistrationRepository.GetByBazaarEventId(command.BazaarEventId, cancellationToken);
        if (registrations.Length > 0)
        {
            return Result.Fail(Event.ValidationDeleteNotPossibleDueToRegistrations);
        }
        return await _eventRepository.Delete(command.BazaarEventId, cancellationToken);
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

    public async ValueTask<BazaarEventWithBilling[]> Handle(GetEventsWithBillingQuery query, CancellationToken cancellationToken)
    {
        var events = await _eventRepository.GetAll(cancellationToken);
        if (events.Length == 0)
        {
            return [];
        }

        Dictionary<Guid, BazaarSeller[]> sellersByEventId;
        {
            var sellers = await _sellerRepository.GetAll(cancellationToken);
            var registrations = await _sellerRegistrationRepository.GetAllByAccepted(cancellationToken);
            var registrationsAcceptedBySellerId = new HashSet<Guid>(registrations.Select(r => r.BazaarSellerId!.Value));
            sellersByEventId = sellers
                .Where(s => registrationsAcceptedBySellerId.Contains(s.Id))
                .GroupBy(s => s.BazaarEventId)
                .ToDictionary(s => s.Key, s => s.ToArray());
        }

        Dictionary<Guid, BazaarBilling[]> billingsByEventId;
        {
            var billings = await _billingRepository.GetAll(cancellationToken);
            billingsByEventId = billings.GroupBy(b => b.BazaarEventId).ToDictionary(b => b.Key, b => b.ToArray());
        }

        Dictionary<Guid, BazaarSellerArticle> articlesById;
        {
            var articles = await _sellerArticleRepository.GetAll(cancellationToken);
            articlesById = articles.ToDictionary(a => a.Id);
        }

        Dictionary<Guid, BazaarBillingArticle[]> billingArticlesByBillingId;
        {
            var billingArticles = await _billingArticleRepository.GetAll(cancellationToken);
            billingArticlesByBillingId = billingArticles.GroupBy(b => b.BazaarBillingId).ToDictionary(b => b.Key, b => b.ToArray());
        }

        var result = new List<BazaarEventWithBilling>(events.Length);
        foreach (var @event in events)
        {
            var eventBillings = billingsByEventId[@event.Id];
            var soldTotal = 0m;
            foreach (var billing in eventBillings.Where(b => b.IsCompleted))
            {
                var eventBillingArticles = billingArticlesByBillingId[billing.Id];
                soldTotal += eventBillingArticles.Sum(b => articlesById[b.BazaarSellerArticleId].Price);
            }
            var commissionTotal = (@event.Commission / 100.0M) * soldTotal;

            result.Add(new(@event, eventBillings.Length, soldTotal, commissionTotal));
        }

        return [.. result];
    }
}
