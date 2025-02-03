using FluentResults;
using GtKram.Application.Services;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Extensions;
using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Application.UseCases.User.Commands;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Handlers;

internal sealed class SellerHandler :
    IQueryHandler<FindRegistrationWithSellerQuery, Result<BazaarSellerRegistrationWithSeller>>,
    IQueryHandler<GetSellerRegistrationWithArticleCountQuery, BazaarSellerRegistrationWithArticleCount[]>,
    IQueryHandler<FindSellerWithRegistrationAndArticlesQuery, Result<BazaarSellerWithRegistrationAndArticles>>,
    ICommandHandler<CreateSellerRegistrationCommand, Result>,
    ICommandHandler<UpdateSellerCommand, Result>,
    ICommandHandler<DeleteSellerRegistrationCommand, Result>,
    ICommandHandler<AcceptSellerRegistrationCommand, Result>,
    ICommandHandler<DenySellerRegistrationCommand, Result>
{
    private readonly IMediator _mediator;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IEmailValidatorService _emailValidatorService;
    private readonly IBazaarSellerRegistrationRepository _sellerRegistrationRepository;
    private readonly IBazaarSellerRepository _sellerRepository;
    private readonly IBazaarSellerArticleRepository _sellerArticleRepository;
    private readonly IBazaarEventRepository _eventRepository;

    public SellerHandler(
        IMediator mediator,
        IUserRepository userRepository,
        IEmailService emailService,
        IEmailValidatorService emailValidatorService,
        IBazaarSellerRegistrationRepository sellerRegistrationRepository,
        IBazaarSellerRepository sellerRepository,
        IBazaarSellerArticleRepository sellerArticleRepository,
        IBazaarEventRepository eventRepository)
    {
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
        var countBySellerId = articles.GroupBy(a => a.BazaarSellerId!.Value).ToDictionary(g => g.Key, g => g.Count());

        return registrations
            .Select(r => new BazaarSellerRegistrationWithArticleCount(
                r,
                r.BazaarSellerId is null ? null : (sellersById.TryGetValue(r.BazaarSellerId.Value, out var seller) ? seller : null),
                r.BazaarSellerId is null ? 0 : (countBySellerId.TryGetValue(r.BazaarSellerId.Value, out var count) ? count : 0)))
            .ToArray();
    }

    public async ValueTask<Result> Handle(CreateSellerRegistrationCommand command, CancellationToken cancellationToken)
    {
        var isValid = await _emailValidatorService.Validate(command.Registration.Email, cancellationToken);
        if (!isValid)
        {
            return Result.Fail("Die E-Mail-Adresse ist ungültig.");
        }

        return await _sellerRegistrationRepository.Create(command.Registration, cancellationToken);
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
                Role = SellerRole.Standard,
                MaxArticleCount = SellerRole.Standard.GetMaxArticleCount()
            };

            var sellerResult = await _sellerRepository.Create(seller, registration.Value.BazaarEventId, userId, cancellationToken);
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
}
