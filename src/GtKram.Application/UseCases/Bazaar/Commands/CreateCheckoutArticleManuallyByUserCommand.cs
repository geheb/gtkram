using GtKram.Domain.Base;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public record struct CreateCheckoutArticleManuallyByUserCommand(Guid UserId, Guid CheckoutId, int SellerNumber, int LabelNumber) : ICommand<Result>;
