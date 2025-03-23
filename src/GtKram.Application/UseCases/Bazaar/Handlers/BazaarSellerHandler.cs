using GtKram.Application.Converter;
using GtKram.Application.Services;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Extensions;
using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Application.UseCases.User.Commands;
using GtKram.Domain.Base;
using GtKram.Domain.Errors;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using Mediator;
using Microsoft.AspNetCore.Identity;

namespace GtKram.Application.UseCases.Bazaar.Handlers;

internal sealed class BazaarSellerHandler :
    IQueryHandler<FindRegistrationWithSellerQuery, Result<BazaarSellerRegistrationWithSeller>>,
    IQueryHandler<GetSellerRegistrationWithArticleCountQuery, BazaarSellerRegistrationWithArticleCount[]>,
    IQueryHandler<FindSellerWithRegistrationAndArticlesQuery, Result<BazaarSellerWithRegistrationAndArticles>>,
    IQueryHandler<GetEventsWithSellerAndArticleCountByUserQuery, BazaarEventWithSellerAndArticleCount[]>,
    IQueryHandler<FindSellerWithEventAndArticlesByUserQuery, Result<BazaarSellerWithEventAndArticles>>,
    IQueryHandler<FindSellerArticleByUserQuery, Result<BazaarSellerArticleWithEvent>>,
    IQueryHandler<FindSellerEventByUserQuery, Result<BazaarEvent>>,
    ICommandHandler<CreateSellerRegistrationCommand, Result>,
    ICommandHandler<UpdateSellerCommand, Result>,
    ICommandHandler<DeleteSellerRegistrationCommand, Result>,
    ICommandHandler<AcceptSellerRegistrationCommand, Result>,
    ICommandHandler<DenySellerRegistrationCommand, Result>,
    ICommandHandler<TakeOverSellerArticlesByUserCommand, Result>,
    ICommandHandler<UpdateSellerArticleByUserCommand, Result>,
    ICommandHandler<DeleteSellerArticleByUserCommand, Result>,
    ICommandHandler<CreateSellerArticleByUserCommand, Result>
{
    private readonly TimeProvider _timeProvider;
    private readonly IdentityErrorDescriber _errorDescriber;
    private readonly IMediator _mediator;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IEmailValidatorService _emailValidatorService;
    private readonly IBazaarSellerRegistrationRepository _sellerRegistrationRepository;
    private readonly IBazaarSellerRepository _sellerRepository;
    private readonly IBazaarSellerArticleRepository _sellerArticleRepository;
    private readonly IBazaarBillingRepository _billingRepository;
    private readonly IBazaarBillingArticleRepository _billingArticleRepository;
    private readonly IBazaarEventRepository _eventRepository;

    public BazaarSellerHandler(
        TimeProvider timeProvider,
        IdentityErrorDescriber errorDescriber,
        IMediator mediator,
        IUserRepository userRepository,
        IEmailService emailService,
        IEmailValidatorService emailValidatorService,
        IBazaarSellerRegistrationRepository sellerRegistrationRepository,
        IBazaarSellerRepository sellerRepository,
        IBazaarSellerArticleRepository sellerArticleRepository,
        IBazaarBillingRepository billingRepository,
        IBazaarBillingArticleRepository billingArticleRepository,
        IBazaarEventRepository eventRepository)
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
        _billingRepository = billingRepository;
        _billingArticleRepository = billingArticleRepository;
        _eventRepository = eventRepository;
    }

    public async ValueTask<Result<BazaarSellerRegistrationWithSeller>> Handle(FindRegistrationWithSellerQuery query, CancellationToken cancellationToken)
    {
        var registration = await _sellerRegistrationRepository.Find(query.SellerRegistrationId, cancellationToken);
        if (registration.IsFailed)
        {
            return registration.ToResult();
        }

        BazaarSeller? seller = null;
        if (registration.Value.BazaarSellerId is not null)
        {
            var result = await _sellerRepository.Find(registration.Value.BazaarSellerId.Value, cancellationToken);
            if (result.IsFailed)
            {
                return result.ToResult();
            }
            seller = result.Value;
        }

        return Result.Ok(new BazaarSellerRegistrationWithSeller(registration.Value, seller));
    }

    public async ValueTask<BazaarSellerRegistrationWithArticleCount[]> Handle(GetSellerRegistrationWithArticleCountQuery query, CancellationToken cancellationToken)
    {
        var registrations = await _sellerRegistrationRepository.GetByBazaarEventId(query.EventId, cancellationToken);
        if (registrations.Length == 0)
        {
            return [];
        }

        var sellers = await _sellerRepository.GetByBazaarEventId(query.EventId, cancellationToken);
        if (sellers.Length == 0)
        {
            return registrations
                .Select(r => new BazaarSellerRegistrationWithArticleCount(r, null, 0))
                .ToArray();
        }

        var sellersById = sellers.ToDictionary(s => s.Id);
        var articles = await _sellerArticleRepository.GetByBazaarSellerId(sellersById.Keys.ToArray(), cancellationToken);
        var countBySellerId = articles.GroupBy(a => a.BazaarSellerId).ToDictionary(g => g.Key, g => g.Count());

        return registrations
            .Select(r => new BazaarSellerRegistrationWithArticleCount(
                r,
                r.BazaarSellerId is null ? null : (sellersById.TryGetValue(r.BazaarSellerId.Value, out var seller) ? seller : null),
                r.BazaarSellerId is null ? 0 : (countBySellerId.TryGetValue(r.BazaarSellerId.Value, out var count) ? count : 0)))
            .ToArray();
    }

    public async ValueTask<Result> Handle(CreateSellerRegistrationCommand command, CancellationToken cancellationToken)
    {
        var @event = await _eventRepository.Find(command.Registration.BazaarEventId, cancellationToken);
        if (@event.IsFailed)
        {
            return @event.ToResult();
        }

        if (command.ShouldValidateEvent)
        {
            var converter = new EventConverter();
            if (converter.IsExpired(@event.Value, _timeProvider))
            {
                return Result.Fail(Event.Expired);
            }

            if (!converter.IsRegisterExpired(@event.Value, _timeProvider))
            {
                return Result.Fail(EventRegistration.Expired);
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
        }

        var seller = await _sellerRegistrationRepository.FindByEmailAndBazaarEventId(
            command.Registration.Email, 
            command.Registration.BazaarEventId,
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

        if (registration.Value.BazaarSellerId is null)
        {
            return Result.Fail(Seller.NotFound);
        }

        var seller = await _sellerRepository.Find(registration.Value.BazaarSellerId!.Value, cancellationToken);
        if (seller.IsFailed)
        {
            return Result.Fail(Internal.InvalidData);
        }
        seller.Value.SellerNumber = command.SellerNumber;
        seller.Value.Role = command.Role;
        seller.Value.MaxArticleCount = command.Role.GetMaxArticleCount();
        seller.Value.CanCreateBillings = command.CanCreateBillings;

        var result = await _sellerRepository.Update(seller.Value, cancellationToken);
        if (result.IsFailed || !command.CanCreateBillings)
        {
            return result;
        }

        return await _userRepository.AddRole(seller.Value.UserId, UserRoleType.Billing, cancellationToken);
    }

    public async ValueTask<Result> Handle(DeleteSellerRegistrationCommand command, CancellationToken cancellationToken)
    {
        var registration = await _sellerRegistrationRepository.Find(command.SellerRegistrationId, cancellationToken);
        if (registration.IsFailed)
        {
            return registration.ToResult();
        }

        var result = await _sellerRegistrationRepository.Delete(command.SellerRegistrationId, cancellationToken);
        if (result.IsFailed || registration.Value.BazaarSellerId is null)
        {
            return result;
        }

        return await _sellerRepository.Delete(registration.Value.BazaarSellerId.Value, cancellationToken);
    }

    public async ValueTask<Result> Handle(AcceptSellerRegistrationCommand command, CancellationToken cancellationToken)
    {
        var registration = await _sellerRegistrationRepository.Find(command.SellerRegistrationId, cancellationToken);
        if (registration.IsFailed)
        {
            return registration.ToResult();
        }

        var @event = await _eventRepository.Find(registration.Value.BazaarEventId, cancellationToken);
        if (@event.IsFailed)
        {
            return @event.ToResult();
        }

        var eventConverter = new EventConverter();
        if (eventConverter.IsExpired(@event.Value, _timeProvider))
        {
            return Result.Fail(Event.Expired);
        }

        if (registration.Value.BazaarSellerId is null)
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

            var seller = new BazaarSeller
            {
                UserId = userId,
                BazaarEventId = @event.Value.Id,
                Role = SellerRole.Standard,
                MaxArticleCount = SellerRole.Standard.GetMaxArticleCount()
            };

            var sellerResult = await _sellerRepository.Create(seller, cancellationToken);
            if (sellerResult.IsFailed)
            {
                return sellerResult.ToResult();
            }

            registration.Value.Accepted = true;
            registration.Value.BazaarSellerId = sellerResult.Value;
            var regResult = await _sellerRegistrationRepository.Update(registration.Value, cancellationToken);
            if (regResult.IsFailed)
            {
                return regResult;
            }
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

        var @event = await _eventRepository.Find(registration.Value.BazaarEventId, cancellationToken);
        if (@event.IsFailed)
        {
            return @event.ToResult();
        }

        var eventConverter = new EventConverter();
        if (eventConverter.IsExpired(@event.Value, _timeProvider))
        {
            return Result.Fail(Event.Expired);
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

    public async ValueTask<Result<BazaarSellerWithRegistrationAndArticles>> Handle(FindSellerWithRegistrationAndArticlesQuery query, CancellationToken cancellationToken)
    {
        var registration = await _sellerRegistrationRepository.Find(query.SellerRegistrationId, cancellationToken);
        if (registration.IsFailed)
        {
            return registration.ToResult();
        }

        if (registration.Value.BazaarSellerId is null)
        {
            return Result.Fail(Seller.NotFound);
        }

        var seller = await _sellerRepository.Find(registration.Value.BazaarSellerId.Value, cancellationToken);
        if (seller.IsFailed)
        {
            return Result.Fail(Internal.InvalidData);
        }

        var articles = await _sellerArticleRepository.GetByBazaarSellerId(seller.Value.Id, cancellationToken);
        if (articles.Length == 0)
        {
            return Result.Ok(new BazaarSellerWithRegistrationAndArticles(seller.Value, registration.Value, []));
        }

        HashSet<Guid> billingCompleted = [];
        Dictionary<Guid, BazaarBillingArticle> billingArticlesBySellerArticleId;
        {
            var billingArticles = await _billingArticleRepository.GetBySellerArticleId([.. articles.Select(a => a.Id)], cancellationToken);
            billingArticlesBySellerArticleId = billingArticles.ToDictionary(b => b.BazaarSellerArticleId);
            if (billingArticles.Length > 0)
            {
                var billings = await _billingRepository.GetById([.. billingArticles.Select(b => b.BazaarBillingId).Distinct()], cancellationToken);
                billingCompleted = new(billings.Where(b => b.IsCompleted).Select(b => b.Id));
            }
        }

        var result = new List<BazaarSellerArticleWithBilling>(articles.Length);
        foreach (var article in articles)
        {
            Guid? billingArticleId = null;
            DateTimeOffset? billingCreatedOn = null;
            var isSold = false;
            if (billingArticlesBySellerArticleId.TryGetValue(article.Id, out var billingArticle))
            {
                billingArticleId = billingArticle.Id;
                billingCreatedOn = billingArticle.CreatedOn;
                isSold = billingCompleted.Contains(billingArticle.BazaarBillingId);
            }
            result.Add(new(article, billingArticleId, billingCreatedOn, isSold, seller.Value.SellerNumber));
        }

        return Result.Ok(new BazaarSellerWithRegistrationAndArticles(seller.Value, registration.Value, [.. result]));
    }

    public async ValueTask<BazaarEventWithSellerAndArticleCount[]> Handle(GetEventsWithSellerAndArticleCountByUserQuery query, CancellationToken cancellationToken)
    {
        var sellers = await _sellerRepository.GetByUserId(query.UserId, cancellationToken);
        if (sellers.Length == 0)
        {
            return [];
        }

        var sellerIds = sellers.Select(s => s.Id).ToArray();
        var registrations = await _sellerRegistrationRepository.GetByBazaarSellerId(sellerIds, cancellationToken);
        if (registrations.Length == 0)
        {
            return [];
        }

        var registrationBySellerId = new HashSet<Guid>(registrations
            .Where(r => r.BazaarSellerId.HasValue && r.Accepted == true)
            .Select(r => r.BazaarSellerId!.Value));

        sellers = [.. sellers.Where(s => registrationBySellerId.Contains(s.Id))];
        if (sellers.Length == 0)
        {
            return [];
        }

        sellerIds = sellers.Select(s => s.Id).ToArray();
        var eventIds = sellers.Select(s => s.BazaarEventId).ToArray();
        var events = await _eventRepository.GetById(eventIds, cancellationToken);
        var eventsById = events.ToDictionary(e => e.Id);

        var articles = await _sellerArticleRepository.GetByBazaarSellerId(sellerIds, cancellationToken);
        var countBySellerId = articles
            .GroupBy(a => a.BazaarSellerId)
            .ToDictionary(g => g.Key, g => g.Count());

        var result = new List<BazaarEventWithSellerAndArticleCount>(sellers.Length);

        foreach (var seller in sellers)
        {
            var @event = eventsById[seller.BazaarEventId];
            if (!countBySellerId.TryGetValue(seller.Id, out var count))
            {
                count = 0;
            }
            result.Add(new(@event, seller, count));
        }

        return result.ToArray();
    }

    public async ValueTask<Result<BazaarSellerWithEventAndArticles>> Handle(FindSellerWithEventAndArticlesByUserQuery query, CancellationToken cancellationToken)
    {
        var seller = await _sellerRepository.Find(query.SellerId, cancellationToken);
        if (seller.IsFailed)
        {
            return seller.ToResult();
        }

        if (seller.Value.UserId != query.UserId)
        {
            return Result.Fail(Internal.InvalidRequest);
        }

        var registration = await _sellerRegistrationRepository.FindByBazaarSellerId(query.SellerId, cancellationToken);
        if (registration.IsFailed ||
            !registration.Value.Accepted.GetValueOrDefault())
        {
            return Result.Fail(Seller.Locked);
        }

        var @event = await _eventRepository.Find(seller.Value.BazaarEventId, cancellationToken);
        if (@event.IsFailed)
        {
            return Result.Fail(Internal.InvalidData);
        }

        var articles = await _sellerArticleRepository.GetByBazaarSellerId(query.SellerId, cancellationToken);
        if (articles.Length == 0)
        {
            return Result.Ok(new BazaarSellerWithEventAndArticles(seller.Value, @event.Value, []));
        }

        HashSet<Guid> billingCompleted = [];
        Dictionary<Guid, BazaarBillingArticle> billingArticlesBySellerArticleId;
        {
            var billingArticles = await _billingArticleRepository.GetBySellerArticleId([.. articles.Select(a => a.Id)], cancellationToken);
            billingArticlesBySellerArticleId = billingArticles.ToDictionary(b => b.BazaarSellerArticleId);
            if (billingArticles.Length > 0)
            {
                var billings = await _billingRepository.GetById([.. billingArticles.Select(b => b.BazaarBillingId).Distinct()], cancellationToken);
                billingCompleted = new(billings.Where(b => b.IsCompleted).Select(b => b.Id));
            }
        }

        var result = new List<BazaarSellerArticleWithBilling>(articles.Length);
        foreach (var article in articles)
        {
            Guid? billingArticleId = null;
            DateTimeOffset? billingCreatedOn = null;
            var isSold = false;
            if (billingArticlesBySellerArticleId.TryGetValue(article.Id, out var billingArticle))
            {
                billingArticleId = billingArticle.Id;
                billingCreatedOn = billingArticle.CreatedOn;
                isSold = billingCompleted.Contains(billingArticle.BazaarBillingId);
            }
            result.Add(new(article, billingArticleId, billingCreatedOn, isSold, seller.Value.SellerNumber));
        }

        return Result.Ok(new BazaarSellerWithEventAndArticles(seller.Value, @event.Value, [.. result]));
    }

    public async ValueTask<Result> Handle(TakeOverSellerArticlesByUserCommand command, CancellationToken cancellationToken)
    {
        var sellers = await _sellerRepository.GetByUserId(command.UserId, cancellationToken);
        if (!sellers.Any(s => s.Id == command.SellerId))
        {
            return Result.Fail(Seller.NotFound);
        }

        var registration = await _sellerRegistrationRepository.FindByBazaarSellerId(command.SellerId, cancellationToken);
        if (registration.IsFailed ||
            !registration.Value.Accepted.GetValueOrDefault())
        {
            return Result.Fail(Seller.Locked);
        }

        BazaarSellerArticle[] articles = [];

        // find the latest articles to take over
        foreach (var seller in sellers
            .Where(s => s.Id != command.SellerId)
            .OrderByDescending(s => s.CreatedOn))
        {
            articles = await _sellerArticleRepository.GetByBazaarSellerId(seller.Id, cancellationToken);
            if (articles.Length > 0) break;
        }

        if (articles.Length == 0)
        {
            return Result.Fail(SellerArticle.Empty);
        }

        var billingArticles = await _billingArticleRepository.GetBySellerArticleId([.. articles.Select(s => s.Id)], cancellationToken);
        if (billingArticles.Length > 0)
        {
            var billingArticlesByArticleId = new HashSet<Guid>(billingArticles.Select(b => b.BazaarSellerArticleId));
            articles = articles.Where(a => !billingArticlesByArticleId.Contains(a.Id)).ToArray();
        }

        if (articles.Length == 0)
        {
            return Result.Fail(SellerArticle.Empty);
        }

        var currentSeller = sellers.First(s => s.Id == command.SellerId);
        var currentArticles = await _sellerArticleRepository.GetByBazaarSellerId(command.SellerId, cancellationToken);
        var currentArticlesHash = new HashSet<string>(currentArticles.Select(c => c.Name + c.Size + c.Price), StringComparer.OrdinalIgnoreCase);

        var currentCount = currentArticles.Length;
        if (currentCount >= currentSeller.MaxArticleCount)
        {
            return Result.Fail(SellerArticle.MaxExceeded);
        }

        var takeOverArticles = new List<BazaarSellerArticle>();
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
            return Result.Fail(SellerArticle.Empty);
        }

        return await _sellerArticleRepository.Create(takeOverArticles.ToArray(), command.SellerId, cancellationToken);
    }

    public async ValueTask<Result<BazaarSellerArticleWithEvent>> Handle(FindSellerArticleByUserQuery query, CancellationToken cancellationToken)
    {
        var article = await _sellerArticleRepository.Find(query.SellerArticleId, cancellationToken);
        if (article.IsFailed)
        {
            return article.ToResult();
        }

        var billingArticle = await _billingArticleRepository.FindBySellerArticleId(article.Value.Id, cancellationToken);

        var seller = await _sellerRepository.Find(article.Value.BazaarSellerId, cancellationToken);
        if (seller.IsFailed)
        {
            return seller.ToResult();
        }

        if (seller.Value.UserId != query.UserId)
        {
            return Result.Fail(Internal.InvalidRequest);
        }

        var registration = await _sellerRegistrationRepository.FindByBazaarSellerId(article.Value.BazaarSellerId, cancellationToken);
        if (registration.IsFailed ||
            !registration.Value.Accepted.GetValueOrDefault())
        {
            return Result.Fail(Seller.Locked);
        }

        var @event = await _eventRepository.Find(seller.Value.BazaarEventId, cancellationToken);
        if (@event.IsFailed)
        {
            return Result.Fail(Internal.InvalidData);
        }

        return Result.Ok(new BazaarSellerArticleWithEvent(article.Value, @event.Value, billingArticle.IsSuccess));
    }

    public async ValueTask<Result> Handle(UpdateSellerArticleByUserCommand command, CancellationToken cancellationToken)
    {
        var article = await _sellerArticleRepository.Find(command.SellerArticleId, cancellationToken);
        if (article.IsFailed)
        {
            return article.ToResult();
        }

        var billingArticle = await _billingArticleRepository.FindBySellerArticleId(article.Value.Id, cancellationToken);
        if (billingArticle.IsSuccess)
        {
            return Result.Fail(SellerArticle.EditFailedDueToBooked);
        }

        var seller = await _sellerRepository.Find(article.Value.BazaarSellerId, cancellationToken);
        if (seller.IsFailed)
        {
            return seller.ToResult();
        }

        if (seller.Value.UserId != command.UserId)
        {
            return Result.Fail(Internal.InvalidRequest);
        }

        var registration = await _sellerRegistrationRepository.FindByBazaarSellerId(article.Value.BazaarSellerId, cancellationToken);
        if (registration.IsFailed ||
            !registration.Value.Accepted.GetValueOrDefault())
        {
            return Result.Fail(Seller.Locked);
        }

        var @event = await _eventRepository.Find(seller.Value.BazaarEventId, cancellationToken);
        if (@event.IsFailed)
        {
            return Result.Fail(Internal.InvalidData);
        }

        var converter = new EventConverter();
        if (converter.IsEditArticlesExpired(@event.Value, _timeProvider))
        {
            return Result.Fail(SellerArticle.EditExpired);
        }

        article.Value.Name = command.Name;
        article.Value.Size = command.Size;
        article.Value.Price = command.Price;

        return await _sellerArticleRepository.Update(article.Value, cancellationToken);
    }

    public async ValueTask<Result> Handle(DeleteSellerArticleByUserCommand command, CancellationToken cancellationToken)
    {
        var article = await _sellerArticleRepository.Find(command.SellerArticleId, cancellationToken);
        if (article.IsFailed)
        {
            return article.ToResult();
        }

        var billingArticle = await _billingArticleRepository.FindBySellerArticleId(article.Value.Id, cancellationToken);
        if (billingArticle.IsSuccess)
        {
            return Result.Fail(SellerArticle.EditFailedDueToBooked);
        }

        var seller = await _sellerRepository.Find(article.Value.BazaarSellerId, cancellationToken);
        if (seller.IsFailed)
        {
            return seller.ToResult();
        }

        if (seller.Value.UserId != command.UserId)
        {
            return Result.Fail(Internal.InvalidRequest);
        }

        var registration = await _sellerRegistrationRepository.FindByBazaarSellerId(article.Value.BazaarSellerId, cancellationToken);
        if (registration.IsFailed ||
            !registration.Value.Accepted.GetValueOrDefault())
        {
            return Result.Fail(Seller.Locked);
        }

        var @event = await _eventRepository.Find(seller.Value.BazaarEventId, cancellationToken);
        if (@event.IsFailed)
        {
            return Result.Fail(Internal.InvalidData);
        }

        var converter = new EventConverter();
        if (converter.IsEditArticlesExpired(@event.Value, _timeProvider))
        {
            return Result.Fail(SellerArticle.EditExpired);
        }

        return await _sellerArticleRepository.Delete(command.SellerArticleId, cancellationToken);
    }

    public async ValueTask<Result<BazaarEvent>> Handle(FindSellerEventByUserQuery query, CancellationToken cancellationToken)
    {
        var seller = await _sellerRepository.Find(query.SellerId, cancellationToken);
        if (seller.IsFailed)
        {
            return seller.ToResult();
        }

        if (seller.Value.UserId != query.UserId)
        {
            return Result.Fail(Internal.InvalidRequest);
        }

        var registration = await _sellerRegistrationRepository.FindByBazaarSellerId(seller.Value.Id, cancellationToken);
        if (registration.IsFailed ||
            !registration.Value.Accepted.GetValueOrDefault())
        {
            return Result.Fail(Seller.Locked);
        }

        var @event = await _eventRepository.Find(seller.Value.BazaarEventId, cancellationToken);
        if (@event.IsFailed)
        {
            return Result.Fail(Internal.InvalidData);
        }

        var converter = new EventConverter();
        if (converter.IsExpired(@event.Value, _timeProvider))
        {
            return Result.Fail(Event.Expired);
        }

        if (converter.IsEditArticlesExpired(@event.Value, _timeProvider))
        {
            return Result.Fail(SellerArticle.EditExpired);
        }

        var count = await _sellerArticleRepository.GetCountByBazaarSellerId(seller.Value.Id, cancellationToken);
        if (count.IsFailed)
        {
            return Result.Fail(SellerArticle.Timeout);
        }

        if (count.Value >= seller.Value.MaxArticleCount)
        {
            return Result.Fail(SellerArticle.MaxExceeded);
        }

        return @event;
    }

    public async ValueTask<Result> Handle(CreateSellerArticleByUserCommand command, CancellationToken cancellationToken)
    {
        var seller = await _sellerRepository.Find(command.SellerId, cancellationToken);
        if (seller.IsFailed)
        {
            return seller.ToResult();
        }

        if (seller.Value.UserId != command.UserId)
        {
            return Result.Fail(Internal.InvalidRequest);
        }

        var registration = await _sellerRegistrationRepository.FindByBazaarSellerId(seller.Value.Id, cancellationToken);
        if (registration.IsFailed ||
            !registration.Value.Accepted.GetValueOrDefault())
        {
            return Result.Fail(Seller.Locked);
        }

        var @event = await _eventRepository.Find(seller.Value.BazaarEventId, cancellationToken);
        if (@event.IsFailed)
        {
            return Result.Fail(Internal.InvalidData);
        }

        var converter = new EventConverter();
        if (converter.IsExpired(@event.Value, _timeProvider))
        {
            return Result.Fail(Event.Expired);
        }

        if (converter.IsEditArticlesExpired(@event.Value, _timeProvider))
        {
            return Result.Fail(SellerArticle.EditExpired);
        }

        var count = await _sellerArticleRepository.GetCountByBazaarSellerId(seller.Value.Id, cancellationToken);
        if (count.IsFailed)
        {
            return Result.Fail(SellerArticle.Timeout);
        }

        if (count.Value >= seller.Value.MaxArticleCount)
        {
            return Result.Fail(SellerArticle.MaxExceeded);
        }

        var model = new BazaarSellerArticle
        {
            BazaarSellerId = command.SellerId,
            Name = command.Name,
            Size = command.Size,
            Price = command.Price
        };

        return await _sellerArticleRepository.Create(model, cancellationToken);
    }
}
