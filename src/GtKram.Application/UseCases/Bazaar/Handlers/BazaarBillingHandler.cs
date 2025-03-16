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

internal sealed class BazaarBillingHandler :
    IQueryHandler<GetEventsWithBillingTotalsQuery, BazaarEventWithBillingTotals[]>,
    IQueryHandler<GetBillingsWithTotalsAndEventQuery, Result<BazaarBillingsWithTotalsAndEvent>>,
    IQueryHandler<GetBillingArticlesWithBillingAndEventQuery, Result<BazaarSellerArticlesWithBillingAndEvent>>,
    IQueryHandler<FindBillingTotalQuery, Result<BazaarBillingTotal>>,
    IQueryHandler<GetEventsWithBillingByUserQuery, BazaarEventWithBillingCount[]>,
    IQueryHandler<GetBillingsWithTotalsAndEventByUserQuery, Result<BazaarBillingsWithTotalsAndEvent>>,
    IQueryHandler<GetBillingArticlesWithBillingAndEventByUserQuery, Result<BazaarSellerArticlesWithBillingAndEvent>>,
    IQueryHandler<FindEventByBillingQuery, Result<BazaarEvent>>,
    ICommandHandler<DeleteBillingArticleCommand, Result>,
    ICommandHandler<DeleteBillingArticleByUserCommand, Result>,
    ICommandHandler<CancelBillingCommand, Result>,
    ICommandHandler<CancelBillingByUserCommand, Result>,
    ICommandHandler<CompleteBillingCommand, Result>,
    ICommandHandler<CompleteBillingByUserCommand, Result>,
    ICommandHandler<CreateBillingByUserCommand, Result<Guid>>,
    ICommandHandler<CreateBillingArticleByUserCommand, Result<Guid>>,
    ICommandHandler<CreateBillingArticleManuallyByUserCommand, Result>
{
    private readonly TimeProvider _timeProvider;
    private readonly IUserRepository _userRepository;
    private readonly IBazaarEventRepository _eventRepository;
    private readonly IBazaarSellerRepository _sellerRepository;
    private readonly IBazaarBillingRepository _billingRepository;
    private readonly IBazaarSellerRegistrationRepository _sellerRegistrationRepository;
    private readonly IBazaarBillingArticleRepository _billingArticleRepository;
    private readonly IBazaarSellerArticleRepository _sellerArticleRepository;

    public BazaarBillingHandler(
        TimeProvider timeProvider,
        IUserRepository userRepository,
        IBazaarEventRepository eventRepository,
        IBazaarSellerRepository sellerRepository,
        IBazaarBillingRepository billingRepository,
        IBazaarSellerRegistrationRepository sellerRegistrationRepository,
        IBazaarBillingArticleRepository billingArticleRepository,
        IBazaarSellerArticleRepository sellerArticleRepository)
    {
        _timeProvider = timeProvider;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _sellerRepository = sellerRepository;
        _billingRepository = billingRepository;
        _sellerRegistrationRepository = sellerRegistrationRepository;
        _billingArticleRepository = billingArticleRepository;
        _sellerArticleRepository = sellerArticleRepository;
    }

    public async ValueTask<Result<BazaarBillingsWithTotalsAndEvent>> Handle(GetBillingsWithTotalsAndEventQuery query, CancellationToken cancellationToken)
    {
        var @event = await _eventRepository.Find(query.EventId, cancellationToken);
        if (@event.IsFailed)
        {
            return @event.ToResult();
        }

        var billings = await _billingRepository.GetByBazaarEventId(query.EventId, cancellationToken);
        if (billings.Length == 0)
        {
            return Result.Ok(new BazaarBillingsWithTotalsAndEvent([], @event.Value));
        }

        Dictionary<Guid, BazaarBillingArticle[]> billingArticlesByBillingId;
        {
            var billingArticles = await _billingArticleRepository.GetAll(cancellationToken);
            billingArticlesByBillingId = billingArticles.GroupBy(b => b.BazaarBillingId).ToDictionary(b => b.Key, b => b.ToArray());
        }

        Dictionary<Guid, BazaarSellerArticle> articlesById;
        {
            var articles = await _sellerArticleRepository.GetAll(cancellationToken);
            articlesById = articles.ToDictionary(a => a.Id);
        }

        Dictionary<Guid, string> usersNameById;
        {
            var users = await _userRepository.GetAll(cancellationToken);
            usersNameById = users.ToDictionary(u => u.Id, u => u.Name);
        }

        var result = new List<BazaarBillingWithTotals>();

        foreach (var billing in billings)
        {
            var total = 0m;
            var articleCount = 0;
            if (billingArticlesByBillingId.TryGetValue(billing.Id, out var billingArticles))
            {
                total = billingArticles.Sum(b => articlesById[b.BazaarSellerArticleId].Price);
                articleCount = billingArticles.Length;
            }

            result.Add(new(billing, usersNameById[billing.UserId], articleCount, total));
        }

        return Result.Ok(new BazaarBillingsWithTotalsAndEvent([.. result], @event.Value));
    }

    public async ValueTask<BazaarEventWithBillingTotals[]> Handle(GetEventsWithBillingTotalsQuery query, CancellationToken cancellationToken)
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

        var result = new List<BazaarEventWithBillingTotals>(events.Length);
        foreach (var @event in events)
        {
            var soldTotal = 0m;
            var billingCount = 0;
            if (billingsByEventId.TryGetValue(@event.Id, out var eventBillings))
            {
                billingCount = eventBillings.Length;
                foreach (var billing in eventBillings.Where(b => b.IsCompleted))
                {
                    var eventBillingArticles = billingArticlesByBillingId[billing.Id];
                    soldTotal += eventBillingArticles.Sum(b => articlesById[b.BazaarSellerArticleId].Price);
                }
            }
            var commissionTotal = (@event.Commission / 100.0M) * soldTotal;

            result.Add(new(@event, billingCount, soldTotal, commissionTotal));
        }

        return [.. result];
    }

    public async ValueTask<Result<BazaarSellerArticlesWithBillingAndEvent>> Handle(GetBillingArticlesWithBillingAndEventQuery query, CancellationToken cancellationToken)
    {
        var billing = await _billingRepository.Find(query.BillingId, cancellationToken);
        if (billing.IsFailed)
        {
            return billing.ToResult();
        }

        var @event = await _eventRepository.Find(billing.Value.BazaarEventId, cancellationToken);
        if (@event.IsFailed)
        {
            return Result.Fail(Internal.InvalidData);
        }

        var billingArticles = await _billingArticleRepository.GetByBazaarBillingId(query.BillingId, cancellationToken);
        if (billingArticles.Length == 0)
        {
            return Result.Ok(new BazaarSellerArticlesWithBillingAndEvent(@event.Value, billing.Value, []));
        }

        Dictionary<Guid, BazaarSeller> sellersById;
        Dictionary<Guid, BazaarSellerArticle> articlesById;
        {
            var ids = billingArticles.Select(b => b.BazaarSellerArticleId).ToArray();
            var sellerArticles = await _sellerArticleRepository.GetById(ids, cancellationToken);
            articlesById = sellerArticles.ToDictionary(s => s.Id);

            ids = sellerArticles.Select(s => s.BazaarSellerId).Distinct().ToArray();
            var sellers = await _sellerRepository.GetById(ids, cancellationToken);
            sellersById = sellers.ToDictionary(s => s.Id);
        }

        var result = new List<BazaarSellerArticleWithBilling>(billingArticles.Length);
        foreach (var billingArticle in billingArticles)
        {
            var article = articlesById[billingArticle.BazaarSellerArticleId];

            result.Add(new(
                article,
                billingArticle.Id,
                billingArticle.CreatedOn,
                billing.Value.IsCompleted,
                sellersById[article.BazaarSellerId].SellerNumber));
        }

        return Result.Ok(new BazaarSellerArticlesWithBillingAndEvent(@event.Value, billing.Value, [.. result]));
    }

    public async ValueTask<Result> Handle(DeleteBillingArticleCommand command, CancellationToken cancellationToken)
    {
        var billingArticle = await _billingArticleRepository.Find(command.Id, cancellationToken);
        if (billingArticle.IsFailed)
        {
            return billingArticle;
        }
        var billing = await _billingRepository.Find(billingArticle.Value.BazaarBillingId, cancellationToken);
        if (billing.IsFailed)
        {
            return Result.Fail(Internal.InvalidData);
        }

        var @event = await _eventRepository.Find(billing.Value.BazaarEventId, cancellationToken);
        if (@event.IsFailed)
        {
            return Result.Fail(Internal.InvalidData);
        }
        var eventConverter = new EventConverter();
        if (eventConverter.IsExpired(@event.Value, _timeProvider))
        {
            return Result.Fail(Event.Expired);
        }
        return await _billingArticleRepository.Delete(command.Id, cancellationToken);
    }

    public async ValueTask<Result> Handle(DeleteBillingArticleByUserCommand command, CancellationToken cancellationToken)
    {
        var billingArticle = await _billingArticleRepository.Find(command.BillingArticleId, cancellationToken);
        if (billingArticle.IsFailed)
        {
            return billingArticle;
        }
        var billing = await _billingRepository.Find(billingArticle.Value.BazaarBillingId, cancellationToken);
        if (billing.IsFailed)
        {
            return Result.Fail(Internal.InvalidData);
        }
        if (billing.Value.Status == BillingStatus.Completed)
        {
            return Result.Fail(Billing.StatusCompleted);
        }
        if (billing.Value.UserId != command.UserId)
        {
            return Result.Fail(Internal.InvalidRequest);
        }
        var @event = await _eventRepository.Find(billing.Value.BazaarEventId, cancellationToken);
        if (@event.IsFailed)
        {
            return Result.Fail(Internal.InvalidData);
        }
        var eventConverter = new EventConverter();
        if (eventConverter.IsExpired(@event.Value, _timeProvider))
        {
            return Result.Fail(Event.Expired);
        }
        return await _billingArticleRepository.Delete(command.BillingArticleId, cancellationToken);
    }

    public async ValueTask<Result> Handle(CancelBillingCommand command, CancellationToken cancellationToken)
    {
        var billing = await _billingRepository.Find(command.Id, cancellationToken);
        if (billing.IsFailed)
        {
            return billing;
        }

        var billingArticle = await _billingArticleRepository.DeleteByBillingId(command.Id, cancellationToken);
        if (billingArticle.IsFailed)
        {
            return billingArticle;
        }

        return await _billingRepository.Delete(command.Id, cancellationToken);
    }

    public async ValueTask<Result> Handle(CancelBillingByUserCommand command, CancellationToken cancellationToken)
    {
        var billing = await _billingRepository.Find(command.BillingId, cancellationToken);
        if (billing.IsFailed)
        {
            return billing;
        }

        if (billing.Value.Status == BillingStatus.Completed)
        {
            return Result.Fail(Billing.StatusCompleted);
        }

        if (billing.Value.UserId != command.UserId)
        {
            return Result.Fail(Internal.InvalidRequest);
        }

        var @event = await _eventRepository.Find(billing.Value.BazaarEventId, cancellationToken);
        if (@event.IsFailed)
        {
            return Result.Fail(Internal.InvalidData);
        }
        var eventConverter = new EventConverter();
        if (eventConverter.IsExpired(@event.Value, _timeProvider))
        {
            return Result.Fail(Event.Expired);
        }

        var billingArticle = await _billingArticleRepository.DeleteByBillingId(command.BillingId, cancellationToken);
        if (billingArticle.IsFailed)
        {
            return billingArticle;
        }

        return await _billingRepository.Delete(command.BillingId, cancellationToken);
    }

    public async ValueTask<Result> Handle(CompleteBillingCommand command, CancellationToken cancellationToken)
    {
        var billing = await _billingRepository.Find(command.Id, cancellationToken);
        if (billing.IsFailed)
        {
            return billing;
        }

        if (billing.Value.Status == BillingStatus.Completed)
        {
            return Result.Fail(Billing.StatusCompleted);
        }

        var articles = await _billingArticleRepository.GetByBazaarBillingId(command.Id, cancellationToken);
        if (articles.Length == 0)
        {
            return Result.Fail(Billing.IsEmpty);
        }

        billing.Value.Status = BillingStatus.Completed;
        return await _billingRepository.Update(billing.Value, cancellationToken); 
    }

    public async ValueTask<Result> Handle(CompleteBillingByUserCommand command, CancellationToken cancellationToken)
    {
        var billing = await _billingRepository.Find(command.BillingId, cancellationToken);
        if (billing.IsFailed)
        {
            return billing;
        }

        if (billing.Value.Status == BillingStatus.Completed)
        {
            return Result.Fail(Billing.StatusCompleted);
        }

        if (billing.Value.UserId != command.UserId)
        {
            return Result.Fail(Internal.InvalidRequest);
        }

        var articles = await _billingArticleRepository.GetByBazaarBillingId(command.BillingId, cancellationToken);
        if (articles.Length == 0)
        {
            return Result.Fail(Billing.IsEmpty);
        }

        billing.Value.Status = BillingStatus.Completed;
        return await _billingRepository.Update(billing.Value, cancellationToken);
    }

    public async ValueTask<Result<BazaarBillingTotal>> Handle(FindBillingTotalQuery query, CancellationToken cancellationToken)
    {
        var billing = await _billingRepository.Find(query.Id, cancellationToken);
        if (billing.IsFailed)
        {
            return billing.ToResult();
        }

        var billingArticles = await _billingArticleRepository.GetByBazaarBillingId(query.Id, cancellationToken);
        if (billingArticles.Length == 0)
        {
            return Result.Ok(new BazaarBillingTotal(0, 0));
        }

        var ids = billingArticles.Select(x => x.BazaarSellerArticleId).ToArray();
        var articles = await _sellerArticleRepository.GetById(ids, cancellationToken);
        return Result.Ok(new BazaarBillingTotal(articles.Length, articles.Sum(a => a.Price)));
    }

    public async ValueTask<BazaarEventWithBillingCount[]> Handle(GetEventsWithBillingByUserQuery query, CancellationToken cancellationToken)
    {
        var sellers = await _sellerRepository.GetByUserId(query.UserId, cancellationToken);
        sellers = sellers.Where(s => s.CanCreateBillings).ToArray();
        if (sellers.Length == 0)
        {
            return [];
        }

        var ids = sellers.Select(s => s.Id).ToArray();
        {
            var registrations = await _sellerRegistrationRepository.GetByBazaarSellerId(ids, cancellationToken);
            var registrationsBySellerId = new HashSet<Guid>(registrations.Where(r => r.Accepted == true).Select(r => r.BazaarSellerId!.Value));
            sellers = sellers.Where(s => registrationsBySellerId.Contains(s.Id)).ToArray();
        }

        if (sellers.Length == 0)
        {
            return [];
        }

        ids = sellers.Select(s => s.BazaarEventId).ToArray();
        var events = await _eventRepository.GetById(ids, cancellationToken);

        Dictionary<Guid, int> billingsByEventId;
        {
            var userBillings = await _billingRepository.GetByUserId(query.UserId, cancellationToken);
            billingsByEventId = userBillings.GroupBy(a => a.BazaarEventId).ToDictionary(a => a.Key, a => a.Count());
        }

        var result = new List<BazaarEventWithBillingCount>();
        foreach (var @event in events)
        {
            if (!billingsByEventId.TryGetValue(@event.Id, out var billingCount))
            {
                billingCount = 0;
            }
            result.Add(new(@event, billingCount));
        }

        return [.. result];
    }

    public async ValueTask<Result<BazaarBillingsWithTotalsAndEvent>> Handle(GetBillingsWithTotalsAndEventByUserQuery query, CancellationToken cancellationToken)
    {
        var @event = await _eventRepository.Find(query.EventId, cancellationToken);
        if (@event.IsFailed)
        {
            return @event.ToResult();
        }

        var billings = await _billingRepository.GetByBazaarEventIdAndUserId(query.EventId, query.UserId, cancellationToken);
        if (billings.Length == 0)
        {
            return Result.Ok(new BazaarBillingsWithTotalsAndEvent([], @event.Value));
        }

        var user = await _userRepository.FindById(query.UserId, cancellationToken);
        if (user.IsFailed)
        {
            return user.ToResult();
        }

        Dictionary<Guid, BazaarBillingArticle[]> billingArticlesByBillingId;
        {
            var billingArticles = await _billingArticleRepository.GetAll(cancellationToken);
            billingArticlesByBillingId = billingArticles.GroupBy(b => b.BazaarBillingId).ToDictionary(b => b.Key, b => b.ToArray());
        }

        Dictionary<Guid, BazaarSellerArticle> articlesById;
        {
            var articles = await _sellerArticleRepository.GetAll(cancellationToken);
            articlesById = articles.ToDictionary(a => a.Id);
        }

        var result = new List<BazaarBillingWithTotals>();

        foreach (var billing in billings)
        {
            var total = 0m;
            var articleCount = 0;
            if (billingArticlesByBillingId.TryGetValue(billing.Id, out var billingArticles))
            {
                total = billingArticles.Sum(b => articlesById[b.BazaarSellerArticleId].Price);
                articleCount = billingArticles.Length;
            }

            result.Add(new(billing, user.Value.Name, articleCount, total));
        }

        return Result.Ok(new BazaarBillingsWithTotalsAndEvent([.. result], @event.Value));
    }

    public async ValueTask<Result<Guid>> Handle(CreateBillingByUserCommand command, CancellationToken cancellationToken)
    {
        var seller = await _sellerRepository.GetByUserIdAndBazaarEventId(command.UserId, command.EventId, cancellationToken);
        if (seller.IsFailed)
        {
            return seller.ToResult();
        }

        var user = await _userRepository.FindById(command.UserId, cancellationToken);
        if (user.IsFailed)
        {
            return Result.Fail(Internal.InvalidData);
        }

        var isManager = user.Value.Roles.Any(r => r == UserRoleType.Manager || r == UserRoleType.Administrator);
        if (!isManager)
        {
            if (!seller.Value.CanCreateBillings)
            {
                return Result.Fail(Seller.BillingNotAllowed);
            }

            var @event = await _eventRepository.Find(command.EventId, cancellationToken);
            if (@event.IsFailed)
            {
                return Result.Fail(Internal.InvalidData);
            }

            var eventConverter = new EventConverter();
            if (eventConverter.IsExpired(@event.Value, _timeProvider))
            {
                return Result.Fail(Event.Expired);
            }
        }

        return await _billingRepository.Create(command.EventId, command.UserId, cancellationToken);
    }

    public async ValueTask<Result<BazaarSellerArticlesWithBillingAndEvent>> Handle(GetBillingArticlesWithBillingAndEventByUserQuery query, CancellationToken cancellationToken)
    {
        var billing = await _billingRepository.Find(query.BillingId, cancellationToken);
        if (billing.IsFailed)
        {
            return billing.ToResult();
        }

        if (billing.Value.UserId != query.UserId)
        {
            return Result.Fail(Billing.NotFound);
        }

        var @event = await _eventRepository.Find(billing.Value.BazaarEventId, cancellationToken);
        if (@event.IsFailed)
        {
            return Result.Fail(Internal.InvalidData);
        }

        var billingArticles = await _billingArticleRepository.GetByBazaarBillingId(query.BillingId, cancellationToken);
        if (billingArticles.Length == 0)
        {
            return Result.Ok(new BazaarSellerArticlesWithBillingAndEvent(@event.Value, billing.Value, []));
        }

        Dictionary<Guid, BazaarSeller> sellersById;
        Dictionary<Guid, BazaarSellerArticle> articlesById;
        {
            var ids = billingArticles.Select(b => b.BazaarSellerArticleId).ToArray();
            var sellerArticles = await _sellerArticleRepository.GetById(ids, cancellationToken);
            articlesById = sellerArticles.ToDictionary(s => s.Id);

            ids = sellerArticles.Select(s => s.BazaarSellerId).Distinct().ToArray();
            var sellers = await _sellerRepository.GetById(ids, cancellationToken);
            sellersById = sellers.ToDictionary(s => s.Id);
        }

        var result = new List<BazaarSellerArticleWithBilling>(billingArticles.Length);
        foreach (var billingArticle in billingArticles)
        {
            var article = articlesById[billingArticle.BazaarSellerArticleId];
            result.Add(new(
                article,
                billingArticle.Id,
                billingArticle.CreatedOn,
                billing.Value.IsCompleted,
                sellersById[article.BazaarSellerId].SellerNumber));
        }

        return Result.Ok(new BazaarSellerArticlesWithBillingAndEvent(@event.Value, billing.Value, [.. result]));

    }

    public async ValueTask<Result<Guid>> Handle(CreateBillingArticleByUserCommand command, CancellationToken cancellationToken)
    {
        var billing = await _billingRepository.Find(command.BillingId, cancellationToken);
        if (billing.IsFailed)
        {
            return billing.ToResult();
        }

        if (billing.Value.UserId != command.UserId)
        {
            return Result.Fail(Internal.InvalidRequest);
        }

        var article = await _sellerArticleRepository.Find(command.SellerArticleId, cancellationToken);
        if (article.IsFailed)
        {
            return article.ToResult();
        }

        var seller = await _sellerRepository.Find(article.Value.BazaarSellerId, cancellationToken);
        if (seller.IsFailed)
        {
            return Result.Fail(Internal.InvalidData);
        }

        if (seller.Value.BazaarEventId != billing.Value.BazaarEventId)
        {
            return Result.Fail(Billing.NotFound);
        }

        var billingArticle = await _billingArticleRepository.FindBySellerArticleId(command.SellerArticleId, cancellationToken);
        if (billingArticle.IsSuccess)
        {
            return Result.Fail(BillingArticle.AlreadyBooked);
        }

        return await _billingArticleRepository.Create(command.BillingId, command.SellerArticleId, cancellationToken);
    }

    public async ValueTask<Result<BazaarEvent>> Handle(FindEventByBillingQuery query, CancellationToken cancellationToken)
    {
        var billing = await _billingRepository.Find(query.BillingId, cancellationToken);
        if (billing.IsFailed)
        {
            return billing.ToResult();
        }

        return await _eventRepository.Find(billing.Value.BazaarEventId, cancellationToken);
    }

    public async ValueTask<Result> Handle(CreateBillingArticleManuallyByUserCommand command, CancellationToken cancellationToken)
    {
        var billing = await _billingRepository.Find(command.BillingId, cancellationToken);
        if (billing.IsFailed)
        {
            return billing;
        }

        if (billing.Value.UserId != command.UserId)
        {
            return Result.Fail(Internal.InvalidRequest);
        }

        var @event = await _eventRepository.Find(billing.Value.BazaarEventId, cancellationToken);
        if (@event.IsFailed)
        {
            return Result.Fail(Internal.InvalidData);
        }

        var eventConverter = new EventConverter();
        if (eventConverter.IsExpired(@event.Value, _timeProvider))
        {
            return Result.Fail(Event.Expired);
        }

        var seller = await _sellerRepository.FindBySellerNumberAndEventId(command.SellerNumber, billing.Value.BazaarEventId, cancellationToken);
        if (seller.IsFailed)
        {
            return seller;
        }

        var article = await _sellerArticleRepository.FindByBazaarSellerIdAndLabelNumber(seller.Value.Id, command.LabelNumber, cancellationToken);
        if (article.IsFailed)
        {
            return article;
        }

        var billingArticle = await _billingArticleRepository.FindBySellerArticleId(article.Value.Id, cancellationToken);
        if (billingArticle.IsSuccess)
        {
            return Result.Fail(BillingArticle.AlreadyBooked);
        }

        return await _billingArticleRepository.Create(billing.Value.Id, article.Value.Id, cancellationToken);
    }
}
