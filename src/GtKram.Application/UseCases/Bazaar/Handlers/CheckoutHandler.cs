using GtKram.Application.Converter;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Domain.Base;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Handlers;

internal sealed class CheckoutHandler :
    IQueryHandler<GetEventWithCheckoutTotalsQuery, EventWithCheckoutTotals[]>,
    IQueryHandler<GetCheckoutWithTotalsAndEventQuery, Result<CheckoutWithTotalsAndEvent>>,
    IQueryHandler<GetArticlesWithCheckoutAndEventQuery, Result<ArticlesWithCheckoutAndEvent>>,
    IQueryHandler<FindCheckoutTotalQuery, Result<CheckoutTotal>>,
    IQueryHandler<GetEventWithCheckoutCountByUserQuery, EventWithCheckoutCount[]>,
    IQueryHandler<GetCheckoutWithTotalsAndEventByUserQuery, Result<CheckoutWithTotalsAndEvent>>,
    IQueryHandler<GetArticlesWithCheckoutAndEventByUserQuery, Result<ArticlesWithCheckoutAndEvent>>,
    IQueryHandler<FindEventByCheckoutQuery, Result<Domain.Models.Event>>,
    ICommandHandler<DeleteCheckoutArticleCommand, Result>,
    ICommandHandler<DeleteCheckoutArticleByUserCommand, Result>,
    ICommandHandler<CancelCheckoutCommand, Result>,
    ICommandHandler<CancelCheckoutByUserCommand, Result>,
    ICommandHandler<CompleteCheckoutCommand, Result>,
    ICommandHandler<CompleteCheckoutByUserCommand, Result>,
    ICommandHandler<CreateCheckoutByUserCommand, Result<Guid>>,
    ICommandHandler<CreateCheckoutArticleByUserCommand, Result>,
    ICommandHandler<CreateCheckoutArticleManuallyByUserCommand, Result>
{
    private readonly TimeProvider _timeProvider;
    private readonly IUsers _users;
    private readonly IEvents _events;
    private readonly ISellers _sellers;
    private readonly ICheckouts _checkouts;
    private readonly ISellerRegistrations _sellerRegistrations;
    private readonly IArticles _articles;

    public CheckoutHandler(
        TimeProvider timeProvider,
        IUsers users,
        IEvents events,
        ISellers sellers,
        ICheckouts checkouts,
        ISellerRegistrations sellerRegistrations,
        IArticles articles)
    {
        _timeProvider = timeProvider;
        _users = users;
        _events = events;
        _sellers = sellers;
        _checkouts = checkouts;
        _sellerRegistrations = sellerRegistrations;
        _articles = articles;
    }

    public async ValueTask<Result<CheckoutWithTotalsAndEvent>> Handle(GetCheckoutWithTotalsAndEventQuery query, CancellationToken cancellationToken)
    {
        var @event = await _events.Find(query.EventId, cancellationToken);
        if (@event.IsFailed)
        {
            return @event.ToResult();
        }

        var checkouts = await _checkouts.GetByEventId(query.EventId, cancellationToken);
        if (checkouts.Length == 0)
        {
            return Result.Ok(new CheckoutWithTotalsAndEvent([], @event.Value));
        }

        Dictionary<Guid, Article> articlesById = [];
        {
            var ids = checkouts.SelectMany(c => c.ArticleIds).ToArray();
            if (ids.Length > 0)
            {
                var articles = await _articles.GetById(ids, cancellationToken);
                articlesById = articles.ToDictionary(a => a.Id);
            }
        }

        Dictionary<Guid, string> usersNameById;
        {
            var users = await _users.GetAll(cancellationToken);
            usersNameById = users.ToDictionary(u => u.Id, u => u.Name);
        }

        var result = new List<CheckoutWithTotals>();

        foreach (var checkout in checkouts)
        {
            var total = checkout.ArticleIds.Sum(id => articlesById[id].Price);
            var articleCount = checkout.ArticleIds.Count;

            result.Add(new(checkout, usersNameById[checkout.IdentityId], articleCount, total));
        }

        return Result.Ok(new CheckoutWithTotalsAndEvent([.. result.OrderByDescending(r => r.Checkout.Created)], @event.Value));
    }

    public async ValueTask<EventWithCheckoutTotals[]> Handle(GetEventWithCheckoutTotalsQuery query, CancellationToken cancellationToken)
    {
        var events = await _events.GetAll(cancellationToken);
        if (events.Length == 0)
        {
            return [];
        }

        Dictionary<Guid, Domain.Models.Seller[]> sellersByEventId;
        {
            var sellers = await _sellers.GetAll(cancellationToken);
            var registrations = await _sellerRegistrations.GetAllByAccepted(cancellationToken);
            var registrationsAcceptedBySellerId = new HashSet<Guid>(registrations.Select(r => r.SellerId!.Value));
            sellersByEventId = sellers
                .Where(s => registrationsAcceptedBySellerId.Contains(s.Id))
                .GroupBy(s => s.EventId)
                .ToDictionary(s => s.Key, s => s.ToArray());
        }

        Dictionary<Guid, Domain.Models.Checkout[]> checkoutsByEventId;
        {
            var checkouts = await _checkouts.GetAll(cancellationToken);
            checkoutsByEventId = checkouts.GroupBy(b => b.EventId).ToDictionary(b => b.Key, b => b.ToArray());
        }

        Dictionary<Guid, Article> articlesById;
        {
            var articles = await _articles.GetAll(cancellationToken);
            articlesById = articles.ToDictionary(a => a.Id);
        }

        var result = new List<EventWithCheckoutTotals>(events.Length);
        foreach (var @event in events)
        {
            var soldTotal = 0m;
            var checkoutCount = 0;
            if (checkoutsByEventId.TryGetValue(@event.Id, out var checkouts))
            {
                checkoutCount = checkouts.Length;

                foreach (var checkout in checkouts.Where(b => b.IsCompleted))
                {
                    soldTotal += checkout.ArticleIds.Sum(id => articlesById[id].Price);
                }
            }
            var commissionTotal = (@event.Commission / 100.0M) * soldTotal;

            result.Add(new(@event, checkoutCount, soldTotal, commissionTotal));
        }

        return [.. result];
    }

    public async ValueTask<Result<ArticlesWithCheckoutAndEvent>> Handle(GetArticlesWithCheckoutAndEventQuery query, CancellationToken cancellationToken)
    {
        var checkout = await _checkouts.Find(query.CheckoutId, cancellationToken);
        if (checkout.IsFailed)
        {
            return checkout.ToResult();
        }

        var @event = await _events.Find(checkout.Value.EventId, cancellationToken);
        if (@event.IsFailed)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidData);
        }

        if (checkout.Value.ArticleIds.Count == 0)
        {
            return Result.Ok(new ArticlesWithCheckoutAndEvent(@event.Value, checkout.Value, []));
        }

        var articles = await _articles.GetById([.. checkout.Value.ArticleIds], cancellationToken);
        if (articles.Length == 0)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidData);
        }

        Dictionary<Guid, Domain.Models.Seller> sellersById;
        {
            var ids = articles.Select(s => s.SellerId).Distinct().ToArray();
            var sellers = await _sellers.GetById(ids, cancellationToken);
            sellersById = sellers.ToDictionary(s => s.Id);
        }

        var result = new List<ArticleWithCheckout>(articles.Length);
        foreach (var article in articles)
        {
            result.Add(new(
                article,
                checkout.Value,
                sellersById[article.SellerId].SellerNumber));
        }

        return Result.Ok(new ArticlesWithCheckoutAndEvent(@event.Value, checkout.Value, [.. result.OrderBy(r => r.Article.Name)]));
    }

    public async ValueTask<Result> Handle(DeleteCheckoutArticleCommand command, CancellationToken cancellationToken)
    {
        var checkout = await _checkouts.Find(command.CheckoutId, cancellationToken);
        if (checkout.IsFailed)
        {
            return checkout;
        }

        var isDeleted = checkout.Value.ArticleIds.Remove(command.ArticleId);
        if (!isDeleted)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidData);
        }

        return await _checkouts.Update(checkout.Value, cancellationToken);
    }

    public async ValueTask<Result> Handle(DeleteCheckoutArticleByUserCommand command, CancellationToken cancellationToken)
    {
        var checkout = await _checkouts.Find(command.CheckoutId, cancellationToken);
        if (checkout.IsFailed)
        {
            return checkout;
        }

        if (checkout.Value.Status == CheckoutStatus.Completed)
        {
            return Result.Fail(Domain.Errors.Checkout.StatusCompleted);
        }
        if (checkout.Value.IdentityId != command.UserId)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidRequest);
        }
        var @event = await _events.Find(checkout.Value.EventId, cancellationToken);
        if (@event.IsFailed)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidData);
        }
        var eventConverter = new EventConverter();
        if (eventConverter.IsExpired(@event.Value, _timeProvider))
        {
            return Result.Fail(Domain.Errors.Event.Expired);
        }

        var isDeleted = checkout.Value.ArticleIds.Remove(command.ArticleId);
        if (!isDeleted)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidData);
        }

        return await _checkouts.Update(checkout.Value, cancellationToken);
    }

    public async ValueTask<Result> Handle(CancelCheckoutCommand command, CancellationToken cancellationToken)
    {
        return await _checkouts.Delete(command.CheckoutId, cancellationToken);
    }

    public async ValueTask<Result> Handle(CancelCheckoutByUserCommand command, CancellationToken cancellationToken)
    {
        var checkout = await _checkouts.Find(command.CheckoutId, cancellationToken);
        if (checkout.IsFailed)
        {
            return checkout;
        }

        if (checkout.Value.Status == CheckoutStatus.Completed)
        {
            return Result.Fail(Domain.Errors.Checkout.StatusCompleted);
        }

        if (checkout.Value.IdentityId != command.UserId)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidRequest);
        }

        var @event = await _events.Find(checkout.Value.EventId, cancellationToken);
        if (@event.IsFailed)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidData);
        }
        var eventConverter = new EventConverter();
        if (eventConverter.IsExpired(@event.Value, _timeProvider))
        {
            return Result.Fail(Domain.Errors.Event.Expired);
        }

        return await _checkouts.Delete(command.CheckoutId, cancellationToken);
    }

    public async ValueTask<Result> Handle(CompleteCheckoutCommand command, CancellationToken cancellationToken)
    {
        var checkout = await _checkouts.Find(command.CheckoutId, cancellationToken);
        if (checkout.IsFailed)
        {
            return checkout;
        }

        if (checkout.Value.Status == CheckoutStatus.Completed)
        {
            return Result.Fail(Domain.Errors.Checkout.StatusCompleted);
        }

        if (checkout.Value.ArticleIds.Count == 0)
        {
            return Result.Fail(Domain.Errors.Checkout.Empty);
        }

        checkout.Value.Status = CheckoutStatus.Completed;
        return await _checkouts.Update(checkout.Value, cancellationToken); 
    }

    public async ValueTask<Result> Handle(CompleteCheckoutByUserCommand command, CancellationToken cancellationToken)
    {
        var checkout = await _checkouts.Find(command.CheckoutId, cancellationToken);
        if (checkout.IsFailed)
        {
            return checkout;
        }

        if (checkout.Value.Status == CheckoutStatus.Completed)
        {
            return Result.Fail(Domain.Errors.Checkout.StatusCompleted);
        }

        if (checkout.Value.IdentityId != command.UserId)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidRequest);
        }

        if (checkout.Value.ArticleIds.Count == 0)
        {
            return Result.Fail(Domain.Errors.Checkout.Empty);
        }

        checkout.Value.Status = CheckoutStatus.Completed;
        return await _checkouts.Update(checkout.Value, cancellationToken);
    }

    public async ValueTask<Result<CheckoutTotal>> Handle(FindCheckoutTotalQuery query, CancellationToken cancellationToken)
    {
        var checkout = await _checkouts.Find(query.CheckoutId, cancellationToken);
        if (checkout.IsFailed)
        {
            return checkout.ToResult();
        }

        if (checkout.Value.ArticleIds.Count == 0)
        {
            return Result.Ok(new CheckoutTotal(0, 0));
        }

        var articles = await _articles.GetById([.. checkout.Value.ArticleIds], cancellationToken);
        if (articles.Length == 0)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidData);
        }

        return Result.Ok(new CheckoutTotal(articles.Length, articles.Sum(a => a.Price)));
    }

    public async ValueTask<EventWithCheckoutCount[]> Handle(GetEventWithCheckoutCountByUserQuery query, CancellationToken cancellationToken)
    {
        var sellers = await _sellers.GetByIdentityId(query.UserId, cancellationToken);
        sellers = sellers.Where(s => s.CanCheckout).ToArray();
        if (sellers.Length == 0)
        {
            return [];
        }

        var ids = sellers.Select(s => s.Id).ToArray();
        {
            var registrations = await _sellerRegistrations.GetBySellerId(ids, cancellationToken);
            var registrationsBySellerId = new HashSet<Guid>(registrations.Where(r => r.IsAccepted == true).Select(r => r.SellerId!.Value));
            sellers = sellers.Where(s => registrationsBySellerId.Contains(s.Id)).ToArray();
        }

        if (sellers.Length == 0)
        {
            return [];
        }

        ids = sellers.Select(s => s.EventId).ToArray();
        var events = await _events.GetById(ids, cancellationToken);

        Dictionary<Guid, int> checkoutsByEventId;
        {
            var userCheckouts = await _checkouts.GetByIdentityId(query.UserId, cancellationToken);
            checkoutsByEventId = userCheckouts.GroupBy(a => a.EventId).ToDictionary(a => a.Key, a => a.Count());
        }

        var result = new List<EventWithCheckoutCount>();
        foreach (var @event in events)
        {
            if (!checkoutsByEventId.TryGetValue(@event.Id, out var count))
            {
                count = 0;
            }
            result.Add(new(@event, count));
        }

        return [.. result];
    }

    public async ValueTask<Result<CheckoutWithTotalsAndEvent>> Handle(GetCheckoutWithTotalsAndEventByUserQuery query, CancellationToken cancellationToken)
    {
        var @event = await _events.Find(query.EventId, cancellationToken);
        if (@event.IsFailed)
        {
            return @event.ToResult();
        }

        var checkouts = await _checkouts.GetByEventIdAndUserId(query.EventId, query.UserId, cancellationToken);
        if (checkouts.Length == 0)
        {
            return Result.Ok(new CheckoutWithTotalsAndEvent([], @event.Value));
        }

        var user = await _users.FindById(query.UserId, cancellationToken);
        if (user.IsError)
        {
            return Result.Fail(user.FirstError.Code, "error");
        }

        Dictionary<Guid, Article> articlesById = [];
        if (checkouts.Any(c => c.ArticleIds.Count > 0))
        {
            var ids = checkouts.SelectMany(c => c.ArticleIds).ToArray();
            var articles = await _articles.GetById(ids, cancellationToken);
            articlesById = articles.ToDictionary(a => a.Id);
        }

        var result = new List<CheckoutWithTotals>();

        foreach (var checkout in checkouts)
        {
            var articleCount = checkout.ArticleIds.Count;
            decimal total = 0;
            if (articleCount > 0)
            {
                total = checkout.ArticleIds.Sum(id => articlesById[id].Price);
            }

            result.Add(new(checkout, user.Value.Name, articleCount, total));
        }

        return Result.Ok(new CheckoutWithTotalsAndEvent([.. result.OrderByDescending(r => r.Checkout.Created)], @event.Value));
    }

    public async ValueTask<Result<Guid>> Handle(CreateCheckoutByUserCommand command, CancellationToken cancellationToken)
    {
        var seller = await _sellers.FindByIdentityIdAndEventId(command.UserId, command.EventId, cancellationToken);
        if (seller.IsFailed)
        {
            return seller.ToResult();
        }

        var user = await _users.FindById(command.UserId, cancellationToken);
        if (user.IsError)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidData);
        }

        var isManager = user.Value.Roles.Any(r => r == UserRoleType.Manager || r == UserRoleType.Administrator);
        if (!isManager)
        {
            if (!seller.Value.CanCheckout)
            {
                return Result.Fail(Domain.Errors.Seller.CheckoutNotAllowed);
            }

            var @event = await _events.Find(command.EventId, cancellationToken);
            if (@event.IsFailed)
            {
                return Result.Fail(Domain.Errors.Internal.InvalidData);
            }

            var eventConverter = new EventConverter();
            if (eventConverter.IsExpired(@event.Value, _timeProvider))
            {
                return Result.Fail(Domain.Errors.Event.Expired);
            }
        }

        return await _checkouts.Create(command.EventId, command.UserId, cancellationToken);
    }

    public async ValueTask<Result<ArticlesWithCheckoutAndEvent>> Handle(GetArticlesWithCheckoutAndEventByUserQuery query, CancellationToken cancellationToken)
    {
        var checkout = await _checkouts.Find(query.CheckoutId, cancellationToken);
        if (checkout.IsFailed)
        {
            return checkout.ToResult();
        }

        if (checkout.Value.IdentityId != query.UserId)
        {
            return Result.Fail(Domain.Errors.Checkout.NotFound);
        }

        var @event = await _events.Find(checkout.Value.EventId, cancellationToken);
        if (@event.IsFailed)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidData);
        }

        if (checkout.Value.ArticleIds.Count == 0)
        {
            return Result.Ok(new ArticlesWithCheckoutAndEvent(@event.Value, checkout.Value, []));
        }

        var articles = await _articles.GetById([.. checkout.Value.ArticleIds], cancellationToken);
        if (articles.Length == 0)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidData);
        }

        Dictionary<Guid, Domain.Models.Seller> sellersById;
        {
            var ids = articles.Select(s => s.SellerId).Distinct().ToArray();
            var sellers = await _sellers.GetById(ids, cancellationToken);
            sellersById = sellers.ToDictionary(s => s.Id);
        }

        var result = new List<ArticleWithCheckout>(articles.Length);
        foreach (var article in articles)
        {
            result.Add(new(
                article,
                checkout.Value,
                sellersById[article.SellerId].SellerNumber));
        }

        return Result.Ok(new ArticlesWithCheckoutAndEvent(@event.Value, checkout.Value, [.. result.OrderBy(r => r.Article.Name)]));

    }

    public async ValueTask<Result> Handle(CreateCheckoutArticleByUserCommand command, CancellationToken cancellationToken)
    {
        var checkout = await _checkouts.Find(command.CheckoutId, cancellationToken);
        if (checkout.IsFailed)
        {
            return checkout.ToResult();
        }

        if (checkout.Value.IdentityId != command.UserId)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidRequest);
        }

        if (checkout.Value.Status == CheckoutStatus.Completed)
        {
            return Result.Fail(Domain.Errors.Checkout.StatusCompleted);
        }

        var article = await _articles.Find(command.SellerArticleId, cancellationToken);
        if (article.IsFailed)
        {
            return article.ToResult();
        }

        var seller = await _sellers.Find(article.Value.SellerId, cancellationToken);
        if (seller.IsFailed)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidData);
        }

        if (seller.Value.EventId != checkout.Value.EventId)
        {
            return Result.Fail(Domain.Errors.Checkout.NotFound);
        }

        var checkouts = await _checkouts.GetByEventId(@checkout.Value.EventId, cancellationToken);
        var articleIds = checkouts.SelectMany(c => c.ArticleIds).ToHashSet();
        if (articleIds.Contains(article.Value.Id))
        {
            return Result.Fail(Domain.Errors.Checkout.AlreadyBooked);
        }

        checkout.Value.ArticleIds.Add(command.SellerArticleId);

        return await _checkouts.Update(checkout.Value, cancellationToken);
    }

    public async ValueTask<Result<Domain.Models.Event>> Handle(FindEventByCheckoutQuery query, CancellationToken cancellationToken)
    {
        var checkout = await _checkouts.Find(query.CheckoutId, cancellationToken);
        if (checkout.IsFailed)
        {
            return checkout.ToResult();
        }

        return await _events.Find(checkout.Value.EventId, cancellationToken);
    }

    public async ValueTask<Result> Handle(CreateCheckoutArticleManuallyByUserCommand command, CancellationToken cancellationToken)
    {
        var checkout = await _checkouts.Find(command.CheckoutId, cancellationToken);
        if (checkout.IsFailed)
        {
            return checkout;
        }

        if (checkout.Value.IdentityId != command.UserId)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidRequest);
        }

        if (checkout.Value.Status == CheckoutStatus.Completed)
        {
            return Result.Fail(Domain.Errors.Checkout.StatusCompleted);
        }

        var @event = await _events.Find(checkout.Value.EventId, cancellationToken);
        if (@event.IsFailed)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidData);
        }

        var eventConverter = new EventConverter();
        if (eventConverter.IsExpired(@event.Value, _timeProvider))
        {
            return Result.Fail(Domain.Errors.Event.Expired);
        }

        var seller = await _sellers.FindByEventIdAndSellerNumber(checkout.Value.EventId, command.SellerNumber, cancellationToken);
        if (seller.IsFailed)
        {
            return seller;
        }

        var article = await _articles.FindBySellerIdAndLabelNumber(seller.Value.Id, command.LabelNumber, cancellationToken);
        if (article.IsFailed)
        {
            return article;
        }
    
        var checkouts = await _checkouts.GetByEventId(@event.Value.Id, cancellationToken);
        var articleIds = checkouts.SelectMany(c => c.ArticleIds).ToHashSet();
        if (articleIds.Contains(article.Value.Id))
        {
            return Result.Fail(Domain.Errors.Checkout.AlreadyBooked);
        }

        checkout.Value.ArticleIds.Add(article.Value.Id);

        return await _checkouts.Update(checkout.Value, cancellationToken);
    }
}
