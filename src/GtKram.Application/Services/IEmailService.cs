using ErrorOr;
using GtKram.Domain.Models;

namespace GtKram.Application.Services;

public interface IEmailService
{
    Task<ErrorOr<Success>> EnqueueConfirmRegistration(User user, string callbackUrl, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> EnqueueResetPassword(User user, string callbackUrl, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> EnqueueChangeEmail(User user, string callbackUrl, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> EnqueueAcceptSeller(Event @event, string email, string name, CancellationToken cancellationToken);
    Task<ErrorOr<Success>> EnqueueDenySeller(Event @event, string email, string name, CancellationToken cancellationToken);
}
