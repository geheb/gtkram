using ErrorOr;
using GtKram.Application.Converter;
using GtKram.Application.Services;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Extensions;
using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Application.UseCases.User.Commands;
using GtKram.Application.UseCases.User.Extensions;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using Mediator;
using Microsoft.AspNetCore.Identity;

namespace GtKram.Application.UseCases.Bazaar.Handlers;

internal sealed class SellerHandler :
    IQueryHandler<FindRegistrationWithSellerQuery, ErrorOr<SellerRegistrationWithSeller>>,
    IQueryHandler<GetSellerRegistrationWithArticleCountQuery, SellerRegistrationWithArticleCount[]>,
    IQueryHandler<FindSellerWithRegistrationAndArticlesQuery, ErrorOr<SellerWithRegistrationAndArticles>>,
    IQueryHandler<GetEventsWithSellerAndArticleCountByUserQuery, EventWithSellerAndArticleCount[]>,
    IQueryHandler<FindSellerWithEventAndArticlesByUserQuery, ErrorOr<SellerWithEventAndArticles>>,
    IQueryHandler<FindArticleByUserQuery, ErrorOr<ArticleWithEvent>>,
    IQueryHandler<FindSellerEventByUserQuery, ErrorOr<Domain.Models.Event>>,
    ICommandHandler<CreateSellerRegistrationCommand, ErrorOr<Success>>,
    ICommandHandler<UpdateSellerCommand, ErrorOr<Success>>,
    ICommandHandler<DeleteSellerRegistrationCommand, ErrorOr<Success>>,
    ICommandHandler<AcceptSellerRegistrationCommand, ErrorOr<Success>>,
    ICommandHandler<DenySellerRegistrationCommand, ErrorOr<Success>>,
    ICommandHandler<TakeOverSellerArticlesByUserCommand, ErrorOr<Success>>,
    ICommandHandler<UpdateArticleByUserCommand, ErrorOr<Success>>,
    ICommandHandler<DeleteArticleByUserCommand, ErrorOr<Success>>,
    ICommandHandler<CreateArticleByUserCommand, ErrorOr<Success>>
{
    private readonly TimeProvider _timeProvider;
    private readonly IdentityErrorDescriber _errorDescriber;
    private readonly IMediator _mediator;
    private readonly IUsers _users;
    private readonly IEmailService _emailService;
    private readonly IEmailValidator _emailValidatorService;
    private readonly ISellerRegistrations _sellerRegistrations;
    private readonly ISellers _sellers;
    private readonly IArticles _articles;
    private readonly ICheckouts _checkouts;
    private readonly IEvents _events;

    public SellerHandler(
        TimeProvider timeProvider,
        IdentityErrorDescriber errorDescriber,
        IMediator mediator,
        IUsers users,
        IEmailService emailService,
        IEmailValidator emailValidatorService,
        ISellerRegistrations sellerRegistrations,
        ISellers sellers,
        IArticles articles,
        ICheckouts checkouts,
        IEvents events)
    {
        _timeProvider = timeProvider;
        _errorDescriber = errorDescriber;
        _mediator = mediator;
        _users = users;
        _emailService = emailService;
        _emailValidatorService = emailValidatorService;
        _sellerRegistrations = sellerRegistrations;
        _sellers = sellers;
        _articles = articles;
        _checkouts = checkouts;
        _events = events;
    }

    public async ValueTask<ErrorOr<SellerRegistrationWithSeller>> Handle(FindRegistrationWithSellerQuery query, CancellationToken cancellationToken)
    {
        var registration = await _sellerRegistrations.Find(query.SellerRegistrationId, cancellationToken);
        if (registration.IsError)
        {
            return registration.Errors;
        }

        Domain.Models.Seller? seller = null;
        if (registration.Value.SellerId is not null)
        {
            var result = await _sellers.Find(registration.Value.SellerId.Value, cancellationToken);
            if (result.IsError)
            {
                return result.Errors;
            }
            seller = result.Value;
        }

        return new SellerRegistrationWithSeller(registration.Value, seller);
    }

    public async ValueTask<SellerRegistrationWithArticleCount[]> Handle(GetSellerRegistrationWithArticleCountQuery query, CancellationToken cancellationToken)
    {
        var registrations = await _sellerRegistrations.GetByEventId(query.EventId, cancellationToken);
        if (registrations.Length == 0)
        {
            return [];
        }

        var sellers = await _sellers.GetByEventId(query.EventId, cancellationToken);
        if (sellers.Length == 0)
        {
            return registrations
                .Select(r => new SellerRegistrationWithArticleCount(r, null, 0))
                .ToArray();
        }

        var sellersById = sellers.ToDictionary(s => s.Id);
        var articles = await _articles.GetBySellerId(sellersById.Keys.ToArray(), cancellationToken);
        var countBySellerId = articles.GroupBy(a => a.SellerId).ToDictionary(g => g.Key, g => g.Count());

        return registrations
            .Select(r => new SellerRegistrationWithArticleCount(
                r,
                r.SellerId is null ? null : (sellersById.TryGetValue(r.SellerId.Value, out var seller) ? seller : null),
                r.SellerId is null ? 0 : (countBySellerId.TryGetValue(r.SellerId.Value, out var count) ? count : 0)))
            .OrderBy(r => r.Registration.Name)
            .ToArray();
    }

    public async ValueTask<ErrorOr<Success>> Handle(CreateSellerRegistrationCommand command, CancellationToken cancellationToken)
    {
        var @event = await _events.Find(command.Registration.EventId, cancellationToken);
        if (@event.IsError)
        {
            return @event.Errors;
        }

        if (command.ShouldValidateEvent)
        {
            var converter = new EventConverter();
            if (converter.IsExpired(@event.Value, _timeProvider))
            {
                return Domain.Errors.Event.Expired;
            }

            if (!converter.IsRegisterExpired(@event.Value, _timeProvider))
            {
                return Domain.Errors.SellerRegistration.IsExpired;
            }

            if (@event.Value.HasRegistrationsLocked)
            {
                return Domain.Errors.SellerRegistration.IsLocked;
            }

            var count = await _sellerRegistrations.GetCountByEventId(@event.Value.Id, cancellationToken);
            if (count.IsError)
            {
                return count.Errors;
            }

            if (count.Value >= @event.Value.MaxSellers)
            {
                return Domain.Errors.SellerRegistration.LimitExceeded;
            }
        }

        var seller = await _sellerRegistrations.FindByEventIdAndEmail(
            command.Registration.EventId,
            command.Registration.Email,
            cancellationToken);

        if (seller.IsError)
        {
            var isValid = await _emailValidatorService.Validate(command.Registration.Email, cancellationToken);
            if (!isValid)
            {
                return _errorDescriber.InvalidEmail(command.Registration.Email).ToError();
            }

            return await _sellerRegistrations.Create(command.Registration, cancellationToken);
        }
        else
        {
            seller.Value.Name = command.Registration.Name;
            seller.Value.Phone = command.Registration.Phone;
            seller.Value.ClothingType = command.Registration.ClothingType;
            seller.Value.PreferredType = command.Registration.PreferredType;

            return await _sellerRegistrations.Update(seller.Value, cancellationToken);
        }
    }

    public async ValueTask<ErrorOr<Success>> Handle(UpdateSellerCommand command, CancellationToken cancellationToken)
    {
        var registration = await _sellerRegistrations.Find(command.SellerRegistrationId, cancellationToken);
        if (registration.IsError)
        {
            return registration.Errors;
        }

        if (registration.Value.SellerId is null)
        {
            return Domain.Errors.Seller.NotFound;
        }

        var seller = await _sellers.Find(registration.Value.SellerId!.Value, cancellationToken);
        if (seller.IsError)
        {
            return Domain.Errors.Internal.InvalidData;
        }
        seller.Value.SellerNumber = command.SellerNumber;
        seller.Value.Role = command.Role;
        seller.Value.MaxArticleCount = command.Role.GetMaxArticleCount();
        seller.Value.CanCheckout = command.CanCheckout;

        var result = await _sellers.Update(seller.Value, cancellationToken);
        if (result.IsError)
        {
            return result;
        }

        if (command.CanCheckout || command.Role != SellerRole.Standard)
        {
            var roles = new List<UserRoleType> { UserRoleType.Helper };
            if (command.CanCheckout)
            {
                roles.Add(UserRoleType.Checkout);
            }
            result = await _users.AddRoles(seller.Value.IdentityId, [.. roles], cancellationToken);
        }

        return result;
    }

    public async ValueTask<ErrorOr<Success>> Handle(DeleteSellerRegistrationCommand command, CancellationToken cancellationToken)
    {
        var registration = await _sellerRegistrations.Find(command.SellerRegistrationId, cancellationToken);
        if (registration.IsError)
        {
            return registration.Errors;
        }

        var result = await _sellerRegistrations.Delete(command.SellerRegistrationId, cancellationToken);
        if (result.IsError || registration.Value.SellerId is null)
        {
            return result;
        }

        return await _sellers.Delete(registration.Value.SellerId.Value, cancellationToken);
    }

    public async ValueTask<ErrorOr<Success>> Handle(AcceptSellerRegistrationCommand command, CancellationToken cancellationToken)
    {
        var registration = await _sellerRegistrations.Find(command.SellerRegistrationId, cancellationToken);
        if (registration.IsError)
        {
            return registration.Errors;
        }

        var @event = await _events.Find(registration.Value.EventId, cancellationToken);
        if (@event.IsError)
        {
            return @event.Errors;
        }

        var eventConverter = new EventConverter();
        if (eventConverter.IsExpired(@event.Value, _timeProvider))
        {
            return Domain.Errors.Event.Expired;
        }

        if (registration.Value.SellerId is null)
        {
            Guid userId;
            var user = await _users.FindByEmail(registration.Value.Email, cancellationToken);
            if (!user.IsError)
            {
                var resultUser = await _users.AddRoles(user.Value.Id, [UserRoleType.Seller], cancellationToken);
                if (resultUser.IsError)
                {
                    return resultUser.Errors;
                }
                userId = user.Value.Id;
            }
            else
            {
                var userResult = await _mediator.Send(new CreateUserCommand(
                    registration.Value.Name, registration.Value.Email, [UserRoleType.Seller], command.ConfirmUserCallbackUrl),
                    cancellationToken);

                if (userResult.IsError)
                {
                    return userResult.Errors;
                }

                userId = userResult.Value;
            }

            var seller = new Domain.Models.Seller
            {
                IdentityId = userId,
                EventId = @event.Value.Id,
                Role = SellerRole.Standard,
                MaxArticleCount = SellerRole.Standard.GetMaxArticleCount()
            };

            var sellerResult = await _sellers.Create(seller, cancellationToken);
            if (sellerResult.IsError)
            {
                return sellerResult.Errors;
            }

            registration.Value.SellerId = sellerResult.Value;
        }

        registration.Value.IsAccepted = true;
            
        var regResult = await _sellerRegistrations.Update(registration.Value, cancellationToken);
        if (regResult.IsError)
        {
            return regResult;
        }

        var result = await _emailService.EnqueueAcceptSeller(
            @event.Value,
            registration.Value.Email,
            registration.Value.Name,
            cancellationToken);

        return result;
    }

    public async ValueTask<ErrorOr<Success>> Handle(DenySellerRegistrationCommand command, CancellationToken cancellationToken)
    {
        var registration = await _sellerRegistrations.Find(command.SellerRegistrationId, cancellationToken);
        if (registration.IsError)
        {
            return registration.Errors;
        }

        var @event = await _events.Find(registration.Value.EventId, cancellationToken);
        if (@event.IsError)
        {
            return @event.Errors;
        }

        var eventConverter = new EventConverter();
        if (eventConverter.IsExpired(@event.Value, _timeProvider))
        {
            return Domain.Errors.Event.Expired;
        }

        registration.Value.IsAccepted = false;

        var regResult = await _sellerRegistrations.Update(registration.Value, cancellationToken);
        if (regResult.IsError)
        {
            return regResult;
        }

        var result = await _emailService.EnqueueDenySeller(
            @event.Value,
            registration.Value.Email,
            registration.Value.Name,
            cancellationToken);

        return result;
    }

    public async ValueTask<ErrorOr<SellerWithRegistrationAndArticles>> Handle(FindSellerWithRegistrationAndArticlesQuery query, CancellationToken cancellationToken)
    {
        var registration = await _sellerRegistrations.Find(query.SellerRegistrationId, cancellationToken);
        if (registration.IsError)
        {
            return registration.Errors;
        }

        if (registration.Value.SellerId is null)
        {
            return Domain.Errors.Seller.NotFound;
        }

        var seller = await _sellers.Find(registration.Value.SellerId.Value, cancellationToken);
        if (seller.IsError)
        {
            return Domain.Errors.Internal.InvalidData;
        }

        var articles = await _articles.GetBySellerId(seller.Value.Id, cancellationToken);
        if (articles.Length == 0)
        {
            return new SellerWithRegistrationAndArticles(seller.Value, registration.Value, []);
        }

        Dictionary<Guid, Checkout> checkoutByArticleId = [];
        {
            var checkouts = await _checkouts.GetByEventId(seller.Value.EventId, cancellationToken);
            foreach (var checkout in checkouts)
            {
                foreach (var id in checkout.ArticleIds)
                {
                    checkoutByArticleId[id] = checkout;
                }
            }
        }

        var result = new List<ArticleWithCheckout>(articles.Length);
        foreach (var article in articles)
        {
            var checkout = checkoutByArticleId.TryGetValue(article.Id, out var c) ? c : null;
            result.Add(new(article, checkout, seller.Value.SellerNumber));
        }

        return new SellerWithRegistrationAndArticles(seller.Value, registration.Value, [.. result]);
    }

    public async ValueTask<EventWithSellerAndArticleCount[]> Handle(GetEventsWithSellerAndArticleCountByUserQuery query, CancellationToken cancellationToken)
    {
        var sellers = await _sellers.GetByIdentityId(query.UserId, cancellationToken);
        if (sellers.Length == 0)
        {
            return [];
        }

        var sellerIds = sellers.Select(s => s.Id).ToArray();
        var registrations = await _sellerRegistrations.GetBySellerId(sellerIds, cancellationToken);
        if (registrations.Length == 0)
        {
            return [];
        }

        var registrationBySellerId = new HashSet<Guid>(registrations
            .Where(r => r.SellerId.HasValue && r.IsAccepted == true)
            .Select(r => r.SellerId!.Value));

        sellers = [.. sellers.Where(s => registrationBySellerId.Contains(s.Id))];
        if (sellers.Length == 0)
        {
            return [];
        }

        sellerIds = sellers.Select(s => s.Id).ToArray();
        var eventIds = sellers.Select(s => s.EventId).ToArray();
        var events = await _events.GetById(eventIds, cancellationToken);
        var eventsById = events.ToDictionary(e => e.Id);

        var articles = await _articles.GetBySellerId(sellerIds, cancellationToken);
        var countBySellerId = articles
            .GroupBy(a => a.SellerId)
            .ToDictionary(g => g.Key, g => g.Count());

        var result = new List<EventWithSellerAndArticleCount>(sellers.Length);

        foreach (var seller in sellers)
        {
            var @event = eventsById[seller.EventId];
            if (!countBySellerId.TryGetValue(seller.Id, out var count))
            {
                count = 0;
            }
            result.Add(new(@event, seller, count));
        }

        return result.OrderByDescending(r => r.Event.Start).ToArray();
    }

    public async ValueTask<ErrorOr<SellerWithEventAndArticles>> Handle(FindSellerWithEventAndArticlesByUserQuery query, CancellationToken cancellationToken)
    {
        var seller = await _sellers.Find(query.SellerId, cancellationToken);
        if (seller.IsError)
        {
            return seller.Errors;
        }

        if (seller.Value.IdentityId != query.UserId)
        {
            return Domain.Errors.Internal.InvalidRequest;
        }

        var registration = await _sellerRegistrations.FindBySellerId(query.SellerId, cancellationToken);
        if (registration.IsError ||
            !registration.Value.IsAccepted.GetValueOrDefault())
        {
            return Domain.Errors.Seller.Locked;
        }

        var @event = await _events.Find(seller.Value.EventId, cancellationToken);
        if (@event.IsError)
        {
            return Domain.Errors.Internal.InvalidData;
        }

        var articles = await _articles.GetBySellerId(query.SellerId, cancellationToken);
        if (articles.Length == 0)
        {
            return new SellerWithEventAndArticles(seller.Value, @event.Value, []);
        }

        Dictionary<Guid, Checkout> checkoutByArticleId = [];
        {
            var checkouts = await _checkouts.GetByEventId(seller.Value.EventId, cancellationToken);
            foreach (var checkout in checkouts)
            {
                foreach (var id in checkout.ArticleIds)
                {
                    checkoutByArticleId[id] = checkout;
                }
            }
        }

        var result = new List<ArticleWithCheckout>(articles.Length);
        foreach (var article in articles)
        {
            var checkout = checkoutByArticleId.TryGetValue(article.Id, out var c) ? c : null;
            result.Add(new(article, checkout, seller.Value.SellerNumber));
        }

        return new SellerWithEventAndArticles(seller.Value, @event.Value, [.. result]);
    }

    public async ValueTask<ErrorOr<Success>> Handle(TakeOverSellerArticlesByUserCommand command, CancellationToken cancellationToken)
    {
        var sellers = await _sellers.GetByIdentityId(command.UserId, cancellationToken);
        if (!sellers.Any(s => s.Id == command.SellerId))
        {
            return Domain.Errors.Seller.NotFound;
        }

        var registration = await _sellerRegistrations.FindBySellerId(command.SellerId, cancellationToken);
        if (registration.IsError ||
            !registration.Value.IsAccepted.GetValueOrDefault())
        {
            return Domain.Errors.Seller.Locked;
        }

        Article[] articles = [];
        var articlesEventId = Guid.Empty;

        // find the latest articles to take over
        foreach (var seller in sellers
            .Where(s => s.Id != command.SellerId)
            .OrderByDescending(s => s.Created))
        {
            articles = await _articles.GetBySellerId(seller.Id, cancellationToken);
            articlesEventId = seller.EventId;
            if (articles.Length > 0) break;
        }

        if (articles.Length > 0)
        {
            var checkouts = await _checkouts.GetByEventId(articlesEventId, cancellationToken);
            var soldArticles = checkouts.Where(c => c.IsCompleted).SelectMany(c => c.ArticleIds).ToHashSet();
            articles = [.. articles.Where(a => !soldArticles.Contains(a.Id))];
        }

        if (articles.Length == 0)
        {
            return Domain.Errors.SellerArticle.Empty;
        }

        var currentSeller = sellers.First(s => s.Id == command.SellerId);
        var currentArticles = await _articles.GetBySellerId(command.SellerId, cancellationToken);
        var currentArticlesHash = new HashSet<string>(currentArticles.Select(c => c.Name + c.Size + c.Price), StringComparer.OrdinalIgnoreCase);

        var currentCount = currentArticles.Length;
        if (currentCount >= currentSeller.MaxArticleCount)
        {
            return Domain.Errors.SellerArticle.MaxExceeded;
        }

        var takeOverArticles = new List<Article>();
        foreach (var a in articles.OrderBy(a => a.LabelNumber))
        {
            if (currentArticlesHash.Contains(a.Name + a.Size + a.Price)) continue;
            takeOverArticles.Add(new()
            {
                Name = a.Name,
                Size = a.Size,
                Price = a.Price
            });
            if (++currentCount >= currentSeller.MaxArticleCount) break;
        }

        if (takeOverArticles.Count == 0)
        {
            return Domain.Errors.SellerArticle.Empty;
        }

        return await _articles.Create(takeOverArticles.ToArray(), command.SellerId, cancellationToken);
    }

    public async ValueTask<ErrorOr<ArticleWithEvent>> Handle(FindArticleByUserQuery query, CancellationToken cancellationToken)
    {
        var article = await _articles.Find(query.ArticleId, cancellationToken);
        if (article.IsError)
        {
            return article.Errors;
        }

        var seller = await _sellers.Find(article.Value.SellerId, cancellationToken);
        if (seller.IsError)
        {
            return seller.Errors;
        }

        if (seller.Value.IdentityId != query.UserId)
        {
            return Domain.Errors.Internal.InvalidRequest;
        }

        var registration = await _sellerRegistrations.FindBySellerId(article.Value.SellerId, cancellationToken);
        if (registration.IsError ||
            !registration.Value.IsAccepted.GetValueOrDefault())
        {
            return Domain.Errors.Seller.Locked;
        }

        var @event = await _events.Find(seller.Value.EventId, cancellationToken);
        if (@event.IsError)
        {
            return Domain.Errors.Internal.InvalidData;
        }

        var hasBooked = await _checkouts.HasArticle(@event.Value.Id, article.Value.Id, cancellationToken);

        return new ArticleWithEvent(article.Value, @event.Value, hasBooked);
    }

    public async ValueTask<ErrorOr<Success>> Handle(UpdateArticleByUserCommand command, CancellationToken cancellationToken)
    {
        var article = await _articles.Find(command.ArticleId, cancellationToken);
        if (article.IsError)
        {
            return article.Errors;
        }

        var seller = await _sellers.Find(article.Value.SellerId, cancellationToken);
        if (seller.IsError)
        {
            return seller.Errors;
        }

        if (seller.Value.IdentityId != command.UserId)
        {
            return Domain.Errors.Internal.InvalidRequest;
        }

        var registration = await _sellerRegistrations.FindBySellerId(article.Value.SellerId, cancellationToken);
        if (registration.IsError ||
            !registration.Value.IsAccepted.GetValueOrDefault())
        {
            return Domain.Errors.Seller.Locked;
        }

        var @event = await _events.Find(seller.Value.EventId, cancellationToken);
        if (@event.IsError)
        {
            return Domain.Errors.Internal.InvalidData;
        }

        var converter = new EventConverter();
        if (converter.IsEditArticlesExpired(@event.Value, _timeProvider))
        {
            return Domain.Errors.SellerArticle.EditExpired;
        }

        var hasBooked = await _checkouts.HasArticle(@event.Value.Id, article.Value.Id, cancellationToken);
        if (hasBooked)
        {
            return Domain.Errors.SellerArticle.EditFailedDueToBooked;
        }

        article.Value.Name = command.Name;
        article.Value.Size = command.Size;
        article.Value.Price = command.Price;

        return await _articles.Update(article.Value, cancellationToken);
    }

    public async ValueTask<ErrorOr<Success>> Handle(DeleteArticleByUserCommand command, CancellationToken cancellationToken)
    {
        var article = await _articles.Find(command.ArticleId, cancellationToken);
        if (article.IsError)
        {
            return article.Errors;
        }

        var seller = await _sellers.Find(article.Value.SellerId, cancellationToken);
        if (seller.IsError)
        {
            return seller.Errors;
        }

        if (seller.Value.IdentityId != command.UserId)
        {
            return Domain.Errors.Internal.InvalidRequest;
        }

        var registration = await _sellerRegistrations.FindBySellerId(article.Value.SellerId, cancellationToken);
        if (registration.IsError ||
            !registration.Value.IsAccepted.GetValueOrDefault())
        {
            return Domain.Errors.Seller.Locked;
        }

        var @event = await _events.Find(seller.Value.EventId, cancellationToken);
        if (@event.IsError)
        {
            return Domain.Errors.Internal.InvalidData;
        }

        var converter = new EventConverter();
        if (converter.IsEditArticlesExpired(@event.Value, _timeProvider))
        {
            return Domain.Errors.SellerArticle.EditExpired;
        }

        var hasBooked = await _checkouts.HasArticle(@event.Value.Id, article.Value.Id, cancellationToken);
        if (hasBooked)
        {
            return Domain.Errors.SellerArticle.EditFailedDueToBooked;
        }

        return await _articles.Delete(command.ArticleId, cancellationToken);
    }

    public async ValueTask<ErrorOr<Domain.Models.Event>> Handle(FindSellerEventByUserQuery query, CancellationToken cancellationToken)
    {
        var seller = await _sellers.Find(query.SellerId, cancellationToken);
        if (seller.IsError)
        {
            return seller.Errors;
        }

        if (seller.Value.IdentityId != query.UserId)
        {
            return Domain.Errors.Internal.InvalidRequest;
        }

        var registration = await _sellerRegistrations.FindBySellerId(seller.Value.Id, cancellationToken);
        if (registration.IsError ||
            !registration.Value.IsAccepted.GetValueOrDefault())
        {
            return Domain.Errors.Seller.Locked;
        }

        var @event = await _events.Find(seller.Value.EventId, cancellationToken);
        if (@event.IsError)
        {
            return Domain.Errors.Internal.InvalidData;
        }

        var converter = new EventConverter();
        if (converter.IsExpired(@event.Value, _timeProvider))
        {
            return Domain.Errors.Event.Expired;
        }

        if (converter.IsEditArticlesExpired(@event.Value, _timeProvider))
        {
            return Domain.Errors.SellerArticle.EditExpired;
        }

        var count = await _articles.GetCountBySellerId(seller.Value.Id, cancellationToken);
        if (count.IsError)
        {
            return Domain.Errors.SellerArticle.Timeout;
        }

        if (count.Value >= seller.Value.MaxArticleCount)
        {
            return Domain.Errors.SellerArticle.MaxExceeded;
        }

        return @event;
    }

    public async ValueTask<ErrorOr<Success>> Handle(CreateArticleByUserCommand command, CancellationToken cancellationToken)
    {
        var seller = await _sellers.Find(command.SellerId, cancellationToken);
        if (seller.IsError)
        {
            return seller.Errors;
        }

        if (seller.Value.IdentityId != command.UserId)
        {
            return Domain.Errors.Internal.InvalidRequest;
        }

        var registration = await _sellerRegistrations.FindBySellerId(seller.Value.Id, cancellationToken);
        if (registration.IsError ||
            !registration.Value.IsAccepted.GetValueOrDefault())
        {
            return Domain.Errors.Seller.Locked;
        }

        var @event = await _events.Find(seller.Value.EventId, cancellationToken);
        if (@event.IsError)
        {
            return Domain.Errors.Internal.InvalidData;
        }

        var converter = new EventConverter();
        if (converter.IsExpired(@event.Value, _timeProvider))
        {
            return Domain.Errors.Event.Expired;
        }

        if (converter.IsEditArticlesExpired(@event.Value, _timeProvider))
        {
            return Domain.Errors.SellerArticle.EditExpired;
        }

        var count = await _articles.GetCountBySellerId(seller.Value.Id, cancellationToken);
        if (count.IsError)
        {
            return Domain.Errors.SellerArticle.Timeout;
        }

        if (count.Value >= seller.Value.MaxArticleCount)
        {
            return Domain.Errors.SellerArticle.MaxExceeded;
        }

        var model = new Article
        {
            SellerId = command.SellerId,
            Name = command.Name,
            Size = command.Size,
            Price = command.Price
        };

        return await _articles.Create(model, cancellationToken);
    }
}
