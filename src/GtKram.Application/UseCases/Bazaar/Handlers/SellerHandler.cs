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

internal sealed class SellerHandler :
    IQueryHandler<FindRegistrationWithSellerQuery, Result<BazaarSellerRegistrationWithSeller>>,
    IQueryHandler<GetSellerRegistrationWithArticleCountQuery, BazaarSellerRegistrationWithArticleCount[]>,
    IQueryHandler<FindSellerWithRegistrationAndArticlesQuery, Result<BazaarSellerWithRegistrationAndArticles>>,
    IQueryHandler<GetEventsByUserWithSellerAndAricleCountQuery, BazaarEventWithSellerAndArticleCount[]>,
    IQueryHandler<FindSellerWithEventAndArticlesQuery, Result<BazaarSellerWithEventAndArticles>>,
    ICommandHandler<CreateSellerRegistrationCommand, Result>,
    ICommandHandler<UpdateSellerCommand, Result>,
    ICommandHandler<DeleteSellerRegistrationCommand, Result>,
    ICommandHandler<AcceptSellerRegistrationCommand, Result>,
    ICommandHandler<DenySellerRegistrationCommand, Result>,
    ICommandHandler<TakeOverSellerArticlesCommand, Result>
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
    private readonly IBazaarEventRepository _eventRepository;

    public SellerHandler(
        TimeProvider timeProvider,
        IdentityErrorDescriber errorDescriber,
        IMediator mediator,
        IUserRepository userRepository,
        IEmailService emailService,
        IEmailValidatorService emailValidatorService,
        IBazaarSellerRegistrationRepository sellerRegistrationRepository,
        IBazaarSellerRepository sellerRepository,
        IBazaarSellerArticleRepository sellerArticleRepository,
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
        _eventRepository = eventRepository;
    }

    public async ValueTask<Result<BazaarSellerRegistrationWithSeller>> Handle(FindRegistrationWithSellerQuery query, CancellationToken cancellationToken)
    {
        var registration = await _sellerRegistrationRepository.Find(query.Id, cancellationToken);
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
        }

        var seller = await _sellerRegistrationRepository.FindByEmail(command.Registration.Email, cancellationToken);
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

    public async ValueTask<Result> Handle(UpdateSellerCommand command, CancellationToken cancellationToken) =>
        await _sellerRepository.Update(command.Seller, cancellationToken);

    public async ValueTask<Result> Handle(DeleteSellerRegistrationCommand command, CancellationToken cancellationToken)
    {
        var registration = await _sellerRegistrationRepository.Find(command.Id, cancellationToken);
        if (registration.IsFailed)
        {
            return registration.ToResult();
        }

        if (registration.Value.BazaarSellerId is not null)
        {
            var result = await _sellerRepository.Delete(registration.Value.BazaarSellerId.Value, cancellationToken);
            if (result.IsFailed)
            {
                return result;
            }
        }

        return await _sellerRegistrationRepository.Delete(command.Id, cancellationToken);
    }

    public async ValueTask<Result> Handle(AcceptSellerRegistrationCommand command, CancellationToken cancellationToken)
    {
        var registration = await _sellerRegistrationRepository.Find(command.Id, cancellationToken);
        if (registration.IsFailed)
        {
            return registration.ToResult();
        }

        var @event = await _eventRepository.Find(registration.Value.BazaarEventId, cancellationToken);
        if (@event.IsFailed)
        {
            return @event.ToResult();
        }

        if (registration.Value.BazaarSellerId is null)
        {
            Guid userId;
            var user = await _userRepository.FindByEmail(registration.Value.Email, cancellationToken);
            if (user.IsSuccess)
            {
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
                BazaarEventId = @event.Value.Id,
                Role = SellerRole.Standard,
                MaxArticleCount = SellerRole.Standard.GetMaxArticleCount()
            };

            var sellerResult = await _sellerRepository.Create(seller, userId, cancellationToken);
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
        var registration = await _sellerRegistrationRepository.Find(command.Id, cancellationToken);
        if (registration.IsFailed)
        {
            return registration.ToResult();
        }

        registration.Value.Accepted = false;

        var regResult = await _sellerRegistrationRepository.Update(registration.Value, cancellationToken);
        if (regResult.IsFailed)
        {
            return regResult;
        }

        var @event = await _eventRepository.Find(registration.Value.BazaarEventId, cancellationToken);
        if (@event.IsFailed)
        {
            return @event.ToResult();
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
        var seller = await _sellerRepository.Find(query.Id, cancellationToken);
        if (seller.IsFailed)
        {
            return seller.ToResult();
        }

        var registration = await _sellerRegistrationRepository.FindByBazaarSellerId(query.Id, cancellationToken);
        if (registration.IsFailed)
        {
            return registration.ToResult();
        }

        var articles = await _sellerArticleRepository.GetByBazaarSellerId(query.Id, cancellationToken);
        return Result.Ok(new BazaarSellerWithRegistrationAndArticles(seller.Value, registration.Value, articles));
    }

    public async ValueTask<BazaarEventWithSellerAndArticleCount[]> Handle(GetEventsByUserWithSellerAndAricleCountQuery query, CancellationToken cancellationToken)
    {
        var sellers = await _sellerRepository.GetByUserId(query.Id, cancellationToken);
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
        var events = await _eventRepository.Get(eventIds, cancellationToken);
        var eventsById = events.ToDictionary(e => e.Id);

        var articles = await _sellerArticleRepository.GetByBazaarSellerId(sellerIds, cancellationToken);
        var countBySellerId = articles
            .GroupBy(a => a.BazaarSellerId)
            .ToDictionary(g => g.Key, g => g.Count());

        var result = new List<BazaarEventWithSellerAndArticleCount>(sellers.Length);

        foreach (var seller in sellers)
        {
            if (!eventsById.TryGetValue(seller.BazaarEventId, out var @event)) continue;
            if (!countBySellerId.TryGetValue(seller.Id, out var count)) count = 0;

            result.Add(new(@event, seller, count));
        }

        return result.ToArray();
    }

    public async ValueTask<Result<BazaarSellerWithEventAndArticles>> Handle(FindSellerWithEventAndArticlesQuery query, CancellationToken cancellationToken)
    {
        // sanity check
        var sellers = await _sellerRepository.GetByUserId(query.UserId, cancellationToken);
        if (!sellers.Any(s => s.Id == query.Id))
        {
            return Result.Fail(Seller.NotFound);
        }

        var registration = await _sellerRegistrationRepository.FindByBazaarSellerId(query.Id, cancellationToken);
        if (registration.IsFailed ||
            !registration.Value.Accepted.GetValueOrDefault())
        {
            return Result.Fail(Seller.NotFound);
        }

        var articles = await _sellerArticleRepository.GetByBazaarSellerId(query.Id, cancellationToken);
        if (articles.Length == 0)
        {
            return Result.Fail(SellerArticle.NotAvailable);
        }

        var seller = sellers.First(s => s.Id == query.Id);
        var eventId = seller.BazaarEventId;
        var @event = await _eventRepository.Find(eventId, cancellationToken);
        if (@event.IsFailed)
        {
            return Result.Fail(Internal.InvalidData);
        }

        return Result.Ok(new BazaarSellerWithEventAndArticles(seller, @event.Value, articles));
    }

    public async ValueTask<Result> Handle(TakeOverSellerArticlesCommand command, CancellationToken cancellationToken)
    {
        var sellers = await _sellerRepository.GetByUserId(command.UserId, cancellationToken);
        if (!sellers.Any(s => s.Id == command.Id))
        {
            return Result.Fail(Seller.NotFound);
        }

        var registration = await _sellerRegistrationRepository.FindByBazaarSellerId(command.Id, cancellationToken);
        if (registration.IsFailed ||
            !registration.Value.Accepted.GetValueOrDefault())
        {
            return Result.Fail(Seller.NotFound);
        }

        BazaarSellerArticle[] articles = [];

        // find the latest articles to take over
        foreach (var seller in sellers
            .Where(s => s.Id != command.Id)
            .OrderByDescending(s => s.CreatedOn))
        {
            articles = await _sellerArticleRepository.GetByBazaarSellerId(seller.Id, cancellationToken);
            if (articles.Length > 0) break;
        }

        if (articles.Length == 0 || !articles.Any(a => a.Status == SellerArticleStatus.Created))
        {
            return Result.Fail(SellerArticle.NotAvailable);
        }

        var currentSeller = sellers.First(s => s.Id == command.Id);
        var currentArticles = await _sellerArticleRepository.GetByBazaarSellerId(command.Id, cancellationToken);
        if (currentArticles.Length > 0 && 
            currentArticles.Length >= currentSeller.MaxArticleCount)
        {
            return Result.Fail(SellerArticle.MaxExceeded);
        }

        var takeOverArticles = new List<BazaarSellerArticle>();
        var currentCount = currentArticles.Length;

        foreach (var a in articles.Where(a => a.Status == SellerArticleStatus.Created).OrderBy(a => a.LabelNumber))
        {
            takeOverArticles.Add(new()
            {
                Name = a.Name,
                Size = a.Size,
                Price = a.Price,
                Status = SellerArticleStatus.Created
            });
            if (++currentCount >= currentSeller.MaxArticleCount) break;
        }

        return await _sellerArticleRepository.Create(takeOverArticles.ToArray(), command.Id, cancellationToken);
    }
}
