using ErrorOr;
using GtKram.Domain.Models;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct UpdateSellerCommand(
    Guid SellerRegistrationId,
    int SellerNumber,
    SellerRole Role,
    bool CanCheckout) : ICommand<ErrorOr<Success>>;