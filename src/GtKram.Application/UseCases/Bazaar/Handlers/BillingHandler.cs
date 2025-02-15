using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Domain.Base;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Handlers;

internal sealed class BillingHandler :
    IQueryHandler<GetEventsWithBillingTotalsQuery, BazaarEventWithBillingTotals[]>,
    IQueryHandler<GetBazaarBillingsWithTotalsAndEventQuery, Result<BazaarBillingsWithTotalsAndEvent>>
{
    private readonly IUserRepository _userRepository;
    private readonly IBazaarEventRepository _eventRepository;
    private readonly IBazaarSellerRepository _sellerRepository;
    private readonly IBazaarBillingRepository _billingRepository;
    private readonly IBazaarSellerRegistrationRepository _sellerRegistrationRepository;
    private readonly IBazaarBillingArticleRepository _billingArticleRepository;
    private readonly IBazaarSellerArticleRepository _sellerArticleRepository;

    public BillingHandler(
        TimeProvider timeProvider,
        IUserRepository userRepository,
        IBazaarEventRepository eventRepository,
        IBazaarSellerRepository sellerRepository,
        IBazaarBillingRepository billingRepository,
        IBazaarSellerRegistrationRepository sellerRegistrationRepository,
        IBazaarBillingArticleRepository billingArticleRepository,
        IBazaarSellerArticleRepository sellerArticleRepository)
    {
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _sellerRepository = sellerRepository;
        _billingRepository = billingRepository;
        _sellerRegistrationRepository = sellerRegistrationRepository;
        _billingArticleRepository = billingArticleRepository;
        _sellerArticleRepository = sellerArticleRepository;
    }

    public async ValueTask<Result<BazaarBillingsWithTotalsAndEvent>> Handle(GetBazaarBillingsWithTotalsAndEventQuery query, CancellationToken cancellationToken)
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
            var billingArticles = billingArticlesByBillingId[billing.Id];
            var total = billingArticles.Sum(b => articlesById[b.BazaarSellerArticleId].Price);

            result.Add(new(billing, usersNameById[billing.UserId], billingArticles.Length, total));
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
