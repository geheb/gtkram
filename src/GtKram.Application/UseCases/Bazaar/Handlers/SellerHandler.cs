using GtKram.Application.Converter;
using GtKram.Application.Services;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Extensions;
using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Application.UseCases.User.Commands;
using GtKram.Domain.Base;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using Mediator;
using Microsoft.AspNetCore.Identity;

namespace GtKram.Application.UseCases.Bazaar.Handlers;

internal sealed class SellerHandler :
    IQueryHandler<FindRegistrationWithSellerQuery, Result<SellerRegistrationWithSeller>>,
    IQueryHandler<GetSellerRegistrationWithArticleCountQuery, SellerRegistrationWithArticleCount[]>,
    IQueryHandler<FindSellerWithRegistrationAndArticlesQuery, Result<SellerWithRegistrationAndArticles>>,
    IQueryHandler<GetEventsWithSellerAndArticleCountByUserQuery, EventWithSellerAndArticleCount[]>,
    IQueryHandler<FindSellerWithEventAndArticlesByUserQuery, Result<SellerWithEventAndArticles>>,
    IQueryHandler<FindArticleByUserQuery, Result<ArticleWithEvent>>,
    IQueryHandler<FindSellerEventByUserQuery, Result<Domain.Models.Event>>,
    ICommandHandler<CreateSellerRegistrationCommand, Result>,
    ICommandHandler<UpdateSellerCommand, Result>,
    ICommandHandler<DeleteSellerRegistrationCommand, Result>,
    ICommandHandler<AcceptSellerRegistrationCommand, Result>,
    ICommandHandler<DenySellerRegistrationCommand, Result>,
    ICommandHandler<TakeOverSellerArticlesByUserCommand, Result>,
    ICommandHandler<UpdateArticleByUserCommand, Result>,
    ICommandHandler<DeleteArticleByUserCommand, Result>,
    ICommandHandler<CreateArticleByUserCommand, Result>
{
    private readonly TimeProvider _timeProvider;
    private readonly IdentityErrorDescriber _errorDescriber;
    private readonly IMediator _mediator;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IEmailValidatorService _emailValidatorService;
    private readonly ISellerRegistrationRepository _sellerRegistrationRepository;
    private readonly ISellerRepository _sellerRepository;
    private readonly IArticleRepository _sellerArticleRepository;
    private readonly ICheckoutRepository _checkoutRepository;
    private readonly IEventRepository _eventRepository;

    public SellerHandler(
        TimeProvider timeProvider,
        IdentityErrorDescriber errorDescriber,
        IMediator mediator,
        IUserRepository userRepository,
        IEmailService emailService,
        IEmailValidatorService emailValidatorService,
        ISellerRegistrationRepository sellerRegistrationRepository,
        ISellerRepository sellerRepository,
        IArticleRepository sellerArticleRepository,
        ICheckoutRepository checkoutRepository,
        IEventRepository eventRepository)
    {
        _timeProvider = timeProvider;
        _errorDescriber = errorDescriber;
        _mediator = mediator;
        _userRepository = userRepository;
        _emailService = emailService;
        _emailValidatorService = emailValidatorService;
        _sellerRegistrationRepository = sellerRegistrationRepository;
        _sellerRepository = sellerRepository;
        _sellerArticleRepository = sellerArticleRepository;
        _checkoutRepository = checkoutRepository;
        _eventRepository = eventRepository;
    }

    public async ValueTask<Result<SellerRegistrationWithSeller>> Handle(FindRegistrationWithSellerQuery query, CancellationToken cancellationToken)
    {
        var registration = await _sellerRegistrationRepository.Find(query.SellerRegistrationId, cancellationToken);
        if (registration.IsFailed)
        {
            return registration.ToResult();
        }

        Domain.Models.Seller? seller = null;
        if (registration.Value.SellerId is not null)
        {
            var result = await _sellerRepository.Find(registration.Value.SellerId.Value, cancellationToken);
            if (result.IsFailed)
            {
                return result.ToResult();
            }
            seller = result.Value;
        }

        return Result.Ok(new SellerRegistrationWithSeller(registration.Value, seller));
    }

    public async ValueTask<SellerRegistrationWithArticleCount[]> Handle(GetSellerRegistrationWithArticleCountQuery query, CancellationToken cancellationToken)
    {
        var registrations = await _sellerRegistrationRepository.GetByEventId(query.EventId, cancellationToken);
        if (registrations.Length == 0)
        {
            return [];
        }

        var sellers = await _sellerRepository.GetByEventId(query.EventId, cancellationToken);
        if (sellers.Length == 0)
        {
            return registrations
                .Select(r => new SellerRegistrationWithArticleCount(r, null, 0))
                .ToArray();
        }

        var sellersById = sellers.ToDictionary(s => s.Id);
        var articles = await _sellerArticleRepository.GetBySellerId(sellersById.Keys.ToArray(), cancellationToken);
        var countBySellerId = articles.GroupBy(a => a.SellerId).ToDictionary(g => g.Key, g => g.Count());

        return registrations
            .Select(r => new SellerRegistrationWithArticleCount(
                r,
                r.SellerId is null ? null : (sellersById.TryGetValue(r.SellerId.Value, out var seller) ? seller : null),
                r.SellerId is null ? 0 : (countBySellerId.TryGetValue(r.SellerId.Value, out var count) ? count : 0)))
            .OrderBy(r => r.Registration.Name)
            .ToArray();
    }

    public async ValueTask<Result> Handle(CreateSellerRegistrationCommand command, CancellationToken cancellationToken)
    {
        var @event = await _eventRepository.Find(command.Registration.EventId, cancellationToken);
        if (@event.IsFailed)
        {
            return @event.ToResult();
        }

        if (command.ShouldValidateEvent)
        {
            var converter = new EventConverter();
            if (converter.IsExpired(@event.Value, _timeProvider))
            {
                return Result.Fail(Domain.Errors.Event.Expired);
            }

            if (!converter.IsRegisterExpired(@event.Value, _timeProvider))
            {
                return Result.Fail(Domain.Errors.SellerRegistration.IsExpired);
            }

            if (@event.Value.HasRegistrationsLocked)
            {
                return Result.Fail(Domain.Errors.SellerRegistration.IsLocked);
            }

            var count = await _sellerRegistrationRepository.GetCountByEventId(@event.Value.Id, cancellationToken);
            if (count.IsFailed)
            {
                return count.ToResult();
            }

            if (count.Value >= @event.Value.MaxSellers)
            {
                return Result.Fail(Domain.Errors.SellerRegistration.LimitExceeded);
            }
        }

        var seller = await _sellerRegistrationRepository.FindByEventIdAndEmail(
            command.Registration.EventId,
            command.Registration.Email,
            cancellationToken);

        if (seller.IsFailed)
        {
            var isValid = await _emailValidatorService.Validate(command.Registration.Email, cancellationToken);
            if (!isValid)
            {
                var error = _errorDescriber.InvalidEmail(command.Registration.Email);
                return Result.Fail(error.Code, error.Description);
            }

            return await _sellerRegistrationRepository.Create(command.Registration, cancellationToken);
        }
        else
        {
            seller.Value.Name = command.Registration.Name;
            seller.Value.Phone = command.Registration.Phone;
            seller.Value.ClothingType = command.Registration.ClothingType;
            seller.Value.PreferredType = command.Registration.PreferredType;

            return await _sellerRegistrationRepository.Update(seller.Value, cancellationToken);
        }
    }

    public async ValueTask<Result> Handle(UpdateSellerCommand command, CancellationToken cancellationToken)
    {
        var registration = await _sellerRegistrationRepository.Find(command.SellerRegistrationId, cancellationToken);
        if (registration.IsFailed)
        {
            return registration.ToResult();
        }

        if (registration.Value.SellerId is null)
        {
            return Result.Fail(Domain.Errors.Seller.NotFound);
        }

        var seller = await _sellerRepository.Find(registration.Value.SellerId!.Value, cancellationToken);
        if (seller.IsFailed)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidData);
        }
        seller.Value.SellerNumber = command.SellerNumber;
        seller.Value.Role = command.Role;
        seller.Value.MaxArticleCount = command.Role.GetMaxArticleCount();
        seller.Value.CanCheckout = command.CanCheckout;

        var result = await _sellerRepository.Update(seller.Value, cancellationToken);
        if (result.IsFailed || !command.CanCheckout)
        {
            return result;
        }

        return await _userRepository.AddRole(seller.Value.UserId, UserRoleType.Checkout, cancellationToken);
    }

    public async ValueTask<Result> Handle(DeleteSellerRegistrationCommand command, CancellationToken cancellationToken)
    {
        var registration = await _sellerRegistrationRepository.Find(command.SellerRegistrationId, cancellationToken);
        if (registration.IsFailed)
        {
            return registration.ToResult();
        }

        var result = await _sellerRegistrationRepository.Delete(command.SellerRegistrationId, cancellationToken);
        if (result.IsFailed || registration.Value.SellerId is null)
        {
            return result;
        }

        return await _sellerRepository.Delete(registration.Value.SellerId.Value, cancellationToken);
    }

    public async ValueTask<Result> Handle(AcceptSellerRegistrationCommand command, CancellationToken cancellationToken)
    {
        var registration = await _sellerRegistrationRepository.Find(command.SellerRegistrationId, cancellationToken);
        if (registration.IsFailed)
        {
            return registration.ToResult();
        }

        var @event = await _eventRepository.Find(registration.Value.EventId, cancellationToken);
        if (@event.IsFailed)
        {
            return @event.ToResult();
        }

        var eventConverter = new EventConverter();
        if (eventConverter.IsExpired(@event.Value, _timeProvider))
        {
            return Result.Fail(Domain.Errors.Event.Expired);
        }

        if (registration.Value.SellerId is null)
        {
            Guid userId;
            var user = await _userRepository.FindByEmail(registration.Value.Email, cancellationToken);
            if (user.IsSuccess)
            {
                var resultUser = await _userRepository.AddRole(user.Value.Id, UserRoleType.Seller, cancellationToken);
                if (resultUser.IsFailed)
                {
                    return resultUser;
                }
                userId = user.Value.Id;
            }
            else
            {
                var userResult = await _mediator.Send(new CreateUserCommand(
                    registration.Value.Name, registration.Value.Email, [UserRoleType.Seller], command.ConfirmUserCallbackUrl),
                    cancellationToken);

                if (userResult.IsFailed)
                {
                    return user.ToResult();
                }

                userId = userResult.Value;
            }

            var seller = new Domain.Models.Seller
            {
                UserId = userId,
                EventId = @event.Value.Id,
                Role = SellerRole.Standard,
                MaxArticleCount = SellerRole.Standard.GetMaxArticleCount()
            };

            var sellerResult = await _sellerRepository.Create(seller, cancellationToken);
            if (sellerResult.IsFailed)
            {
                return sellerResult.ToResult();
            }

            registration.Value.SellerId = sellerResult.Value;
        }

        registration.Value.Accepted = true;
            
        var regResult = await _sellerRegistrationRepository.Update(registration.Value, cancellationToken);
        if (regResult.IsFailed)
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

    public async ValueTask<Result> Handle(DenySellerRegistrationCommand command, CancellationToken cancellationToken)
    {
        var registration = await _sellerRegistrationRepository.Find(command.SellerRegistrationId, cancellationToken);
        if (registration.IsFailed)
        {
            return registration.ToResult();
        }

        var @event = await _eventRepository.Find(registration.Value.EventId, cancellationToken);
        if (@event.IsFailed)
        {
            return @event.ToResult();
        }

        var eventConverter = new EventConverter();
        if (eventConverter.IsExpired(@event.Value, _timeProvider))
        {
            return Result.Fail(Domain.Errors.Event.Expired);
        }

        registration.Value.Accepted = false;

        var regResult = await _sellerRegistrationRepository.Update(registration.Value, cancellationToken);
        if (regResult.IsFailed)
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

    public async ValueTask<Result<SellerWithRegistrationAndArticles>> Handle(FindSellerWithRegistrationAndArticlesQuery query, CancellationToken cancellationToken)
    {
        var registration = await _sellerRegistrationRepository.Find(query.SellerRegistrationId, cancellationToken);
        if (registration.IsFailed)
        {
            return registration.ToResult();
        }

        if (registration.Value.SellerId is null)
        {
            return Result.Fail(Domain.Errors.Seller.NotFound);
        }

        var seller = await _sellerRepository.Find(registration.Value.SellerId.Value, cancellationToken);
        if (seller.IsFailed)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidData);
        }

        var articles = await _sellerArticleRepository.GetBySellerId(seller.Value.Id, cancellationToken);
        if (articles.Length == 0)
        {
            return Result.Ok(new SellerWithRegistrationAndArticles(seller.Value, registration.Value, []));
        }

        Dictionary<Guid, Checkout> checkoutByArticleId = [];
        {
            var checkouts = await _checkoutRepository.GetByEventId(seller.Value.EventId, cancellationToken);
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

        return Result.Ok(new SellerWithRegistrationAndArticles(seller.Value, registration.Value, [.. result]));
    }

    public async ValueTask<EventWithSellerAndArticleCount[]> Handle(GetEventsWithSellerAndArticleCountByUserQuery query, CancellationToken cancellationToken)
    {
        var sellers = await _sellerRepository.GetByUserId(query.UserId, cancellationToken);
        if (sellers.Length == 0)
        {
            return [];
        }

        var sellerIds = sellers.Select(s => s.Id).ToArray();
        var registrations = await _sellerRegistrationRepository.GetBySellerId(sellerIds, cancellationToken);
        if (registrations.Length == 0)
        {
            return [];
        }

        var registrationBySellerId = new HashSet<Guid>(registrations
            .Where(r => r.SellerId.HasValue && r.Accepted == true)
            .Select(r => r.SellerId!.Value));

        sellers = [.. sellers.Where(s => registrationBySellerId.Contains(s.Id))];
        if (sellers.Length == 0)
        {
            return [];
        }

        sellerIds = sellers.Select(s => s.Id).ToArray();
        var eventIds = sellers.Select(s => s.EventId).ToArray();
        var events = await _eventRepository.GetById(eventIds, cancellationToken);
        var eventsById = events.ToDictionary(e => e.Id);

        var articles = await _sellerArticleRepository.GetBySellerId(sellerIds, cancellationToken);
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

    public async ValueTask<Result<SellerWithEventAndArticles>> Handle(FindSellerWithEventAndArticlesByUserQuery query, CancellationToken cancellationToken)
    {
        var seller = await _sellerRepository.Find(query.SellerId, cancellationToken);
        if (seller.IsFailed)
        {
            return seller.ToResult();
        }

        if (seller.Value.UserId != query.UserId)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidRequest);
        }

        var registration = await _sellerRegistrationRepository.FindBySellerId(query.SellerId, cancellationToken);
        if (registration.IsFailed ||
            !registration.Value.Accepted.GetValueOrDefault())
        {
            return Result.Fail(Domain.Errors.Seller.Locked);
        }

        var @event = await _eventRepository.Find(seller.Value.EventId, cancellationToken);
        if (@event.IsFailed)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidData);
        }

        var articles = await _sellerArticleRepository.GetBySellerId(query.SellerId, cancellationToken);
        if (articles.Length == 0)
        {
            return Result.Ok(new SellerWithEventAndArticles(seller.Value, @event.Value, []));
        }

        Dictionary<Guid, Checkout> checkoutByArticleId = [];
        {
            var checkouts = await _checkoutRepository.GetByEventId(seller.Value.EventId, cancellationToken);
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

        return Result.Ok(new SellerWithEventAndArticles(seller.Value, @event.Value, [.. result]));
    }

    public async ValueTask<Result> Handle(TakeOverSellerArticlesByUserCommand command, CancellationToken cancellationToken)
    {
        var sellers = await _sellerRepository.GetByUserId(command.UserId, cancellationToken);
        if (!sellers.Any(s => s.Id == command.SellerId))
        {
            return Result.Fail(Domain.Errors.Seller.NotFound);
        }

        var registration = await _sellerRegistrationRepository.FindBySellerId(command.SellerId, cancellationToken);
        if (registration.IsFailed ||
            !registration.Value.Accepted.GetValueOrDefault())
        {
            return Result.Fail(Domain.Errors.Seller.Locked);
        }

        Article[] articles = [];
        var articlesEventId = Guid.Empty;

        // find the latest articles to take over
        foreach (var seller in sellers
            .Where(s => s.Id != command.SellerId)
            .OrderByDescending(s => s.Created))
        {
            articles = await _sellerArticleRepository.GetBySellerId(seller.Id, cancellationToken);
            articlesEventId = seller.EventId;
            if (articles.Length > 0) break;
        }

        if (articles.Length > 0)
        {
            var checkouts = await _checkoutRepository.GetByEventId(articlesEventId, cancellationToken);
            var soldArticles = checkouts.Where(c => c.IsCompleted).SelectMany(c => c.ArticleIds).ToHashSet();
            articles = [.. articles.Where(a => !soldArticles.Contains(a.Id))];
        }

        if (articles.Length == 0)
        {
            return Result.Fail(Domain.Errors.SellerArticle.Empty);
        }

        var currentSeller = sellers.First(s => s.Id == command.SellerId);
        var currentArticles = await _sellerArticleRepository.GetBySellerId(command.SellerId, cancellationToken);
        var currentArticlesHash = new HashSet<string>(currentArticles.Select(c => c.Name + c.Size + c.Price), StringComparer.OrdinalIgnoreCase);

        var currentCount = currentArticles.Length;
        if (currentCount >= currentSeller.MaxArticleCount)
        {
            return Result.Fail(Domain.Errors.SellerArticle.MaxExceeded);
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
            return Result.Fail(Domain.Errors.SellerArticle.Empty);
        }

        return await _sellerArticleRepository.Create(takeOverArticles.ToArray(), command.SellerId, cancellationToken);
    }

    public async ValueTask<Result<ArticleWithEvent>> Handle(FindArticleByUserQuery query, CancellationToken cancellationToken)
    {
        var article = await _sellerArticleRepository.Find(query.ArticleId, cancellationToken);
        if (article.IsFailed)
        {
            return article.ToResult();
        }

        var seller = await _sellerRepository.Find(article.Value.SellerId, cancellationToken);
        if (seller.IsFailed)
        {
            return seller.ToResult();
        }

        if (seller.Value.UserId != query.UserId)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidRequest);
        }

        var registration = await _sellerRegistrationRepository.FindBySellerId(article.Value.SellerId, cancellationToken);
        if (registration.IsFailed ||
            !registration.Value.Accepted.GetValueOrDefault())
        {
            return Result.Fail(Domain.Errors.Seller.Locked);
        }

        var @event = await _eventRepository.Find(seller.Value.EventId, cancellationToken);
        if (@event.IsFailed)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidData);
        }

        var hasBooked = await _checkoutRepository.HasArticle(@event.Value.Id, article.Value.Id, cancellationToken);

        return Result.Ok(new ArticleWithEvent(article.Value, @event.Value, hasBooked));
    }

    public async ValueTask<Result> Handle(UpdateArticleByUserCommand command, CancellationToken cancellationToken)
    {
        var article = await _sellerArticleRepository.Find(command.ArticleId, cancellationToken);
        if (article.IsFailed)
        {
            return article.ToResult();
        }

        var seller = await _sellerRepository.Find(article.Value.SellerId, cancellationToken);
        if (seller.IsFailed)
        {
            return seller.ToResult();
        }

        if (seller.Value.UserId != command.UserId)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidRequest);
        }

        var registration = await _sellerRegistrationRepository.FindBySellerId(article.Value.SellerId, cancellationToken);
        if (registration.IsFailed ||
            !registration.Value.Accepted.GetValueOrDefault())
        {
            return Result.Fail(Domain.Errors.Seller.Locked);
        }

        var @event = await _eventRepository.Find(seller.Value.EventId, cancellationToken);
        if (@event.IsFailed)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidData);
        }

        var converter = new EventConverter();
        if (converter.IsEditArticlesExpired(@event.Value, _timeProvider))
        {
            return Result.Fail(Domain.Errors.SellerArticle.EditExpired);
        }

        var hasBooked = await _checkoutRepository.HasArticle(@event.Value.Id, article.Value.Id, cancellationToken);
        if (hasBooked)
        {
            return Result.Fail(Domain.Errors.SellerArticle.EditFailedDueToBooked);
        }

        article.Value.Name = command.Name;
        article.Value.Size = command.Size;
        article.Value.Price = command.Price;

        return await _sellerArticleRepository.Update(article.Value, cancellationToken);
    }

    public async ValueTask<Result> Handle(DeleteArticleByUserCommand command, CancellationToken cancellationToken)
    {
        var article = await _sellerArticleRepository.Find(command.ArticleId, cancellationToken);
        if (article.IsFailed)
        {
            return article.ToResult();
        }

        var seller = await _sellerRepository.Find(article.Value.SellerId, cancellationToken);
        if (seller.IsFailed)
        {
            return seller.ToResult();
        }

        if (seller.Value.UserId != command.UserId)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidRequest);
        }

        var registration = await _sellerRegistrationRepository.FindBySellerId(article.Value.SellerId, cancellationToken);
        if (registration.IsFailed ||
            !registration.Value.Accepted.GetValueOrDefault())
        {
            return Result.Fail(Domain.Errors.Seller.Locked);
        }

        var @event = await _eventRepository.Find(seller.Value.EventId, cancellationToken);
        if (@event.IsFailed)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidData);
        }

        var converter = new EventConverter();
        if (converter.IsEditArticlesExpired(@event.Value, _timeProvider))
        {
            return Result.Fail(Domain.Errors.SellerArticle.EditExpired);
        }

        var hasBooked = await _checkoutRepository.HasArticle(@event.Value.Id, article.Value.Id, cancellationToken);
        if (hasBooked)
        {
            return Result.Fail(Domain.Errors.SellerArticle.EditFailedDueToBooked);
        }

        return await _sellerArticleRepository.Delete(command.ArticleId, cancellationToken);
    }

    public async ValueTask<Result<Domain.Models.Event>> Handle(FindSellerEventByUserQuery query, CancellationToken cancellationToken)
    {
        var seller = await _sellerRepository.Find(query.SellerId, cancellationToken);
        if (seller.IsFailed)
        {
            return seller.ToResult();
        }

        if (seller.Value.UserId != query.UserId)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidRequest);
        }

        var registration = await _sellerRegistrationRepository.FindBySellerId(seller.Value.Id, cancellationToken);
        if (registration.IsFailed ||
            !registration.Value.Accepted.GetValueOrDefault())
        {
            return Result.Fail(Domain.Errors.Seller.Locked);
        }

        var @event = await _eventRepository.Find(seller.Value.EventId, cancellationToken);
        if (@event.IsFailed)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidData);
        }

        var converter = new EventConverter();
        if (converter.IsExpired(@event.Value, _timeProvider))
        {
            return Result.Fail(Domain.Errors.Event.Expired);
        }

        if (converter.IsEditArticlesExpired(@event.Value, _timeProvider))
        {
            return Result.Fail(Domain.Errors.SellerArticle.EditExpired);
        }

        var count = await _sellerArticleRepository.GetCountBySellerId(seller.Value.Id, cancellationToken);
        if (count.IsFailed)
        {
            return Result.Fail(Domain.Errors.SellerArticle.Timeout);
        }

        if (count.Value >= seller.Value.MaxArticleCount)
        {
            return Result.Fail(Domain.Errors.SellerArticle.MaxExceeded);
        }

        return @event;
    }

    public async ValueTask<Result> Handle(CreateArticleByUserCommand command, CancellationToken cancellationToken)
    {
        var seller = await _sellerRepository.Find(command.SellerId, cancellationToken);
        if (seller.IsFailed)
        {
            return seller.ToResult();
        }

        if (seller.Value.UserId != command.UserId)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidRequest);
        }

        var registration = await _sellerRegistrationRepository.FindBySellerId(seller.Value.Id, cancellationToken);
        if (registration.IsFailed ||
            !registration.Value.Accepted.GetValueOrDefault())
        {
            return Result.Fail(Domain.Errors.Seller.Locked);
        }

        var @event = await _eventRepository.Find(seller.Value.EventId, cancellationToken);
        if (@event.IsFailed)
        {
            return Result.Fail(Domain.Errors.Internal.InvalidData);
        }

        var converter = new EventConverter();
        if (converter.IsExpired(@event.Value, _timeProvider))
        {
            return Result.Fail(Domain.Errors.Event.Expired);
        }

        if (converter.IsEditArticlesExpired(@event.Value, _timeProvider))
        {
            return Result.Fail(Domain.Errors.SellerArticle.EditExpired);
        }

        var count = await _sellerArticleRepository.GetCountBySellerId(seller.Value.Id, cancellationToken);
        if (count.IsFailed)
        {
            return Result.Fail(Domain.Errors.SellerArticle.Timeout);
        }

        if (count.Value >= seller.Value.MaxArticleCount)
        {
            return Result.Fail(Domain.Errors.SellerArticle.MaxExceeded);
        }

        var model = new Article
        {
            SellerId = command.SellerId,
            Name = command.Name,
            Size = command.Size,
            Price = command.Price
        };

        return await _sellerArticleRepository.Create(model, cancellationToken);
    }
}
