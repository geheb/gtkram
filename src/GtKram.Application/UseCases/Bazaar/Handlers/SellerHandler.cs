using FluentResults;
using GtKram.Application.Services;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Handlers;

internal sealed class SellerHandler :
    IQueryHandler<FindRegistrationAndSellerQuery, Result<(BazaarSellerRegistration, BazaarSeller?)>>,
    ICommandHandler<CreateSellerRegistrationCommand, Result>,
    ICommandHandler<UpdateSellerCommand, Result>
{
    private readonly IEmailValidatorService _emailValidatorService;
    private readonly IBazaarSellerRegistrationRepository _sellerRegistrationRepository;
    private readonly IBazaarSellerRepository _sellerRepository;

    public SellerHandler(
        IEmailValidatorService emailValidatorService,
        IBazaarSellerRegistrationRepository sellerRegistrationRepository,
        IBazaarSellerRepository sellerRepository)
    {
        _emailValidatorService = emailValidatorService;
        _sellerRegistrationRepository = sellerRegistrationRepository;
        _sellerRepository = sellerRepository;
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

    public async ValueTask<Result<(BazaarSellerRegistration, BazaarSeller?)>> Handle(FindRegistrationAndSellerQuery query, CancellationToken cancellationToken)
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

        return Result.Ok((registration.Value, seller));
    }

    public async ValueTask<Result> Handle(UpdateSellerCommand command, CancellationToken cancellationToken) =>
        await _sellerRepository.Update(command.Seller, cancellationToken);
}
