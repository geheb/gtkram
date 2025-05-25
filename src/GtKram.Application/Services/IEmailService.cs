using GtKram.Domain.Base;
using GtKram.Domain.Models;

namespace GtKram.Application.Services;

public interface IEmailService
{
    Task<Result> EnqueueConfirmRegistration(User user, string callbackUrl, CancellationToken cancellationToken);
    Task<Result> EnqueueResetPassword(User user, string callbackUrl, CancellationToken cancellationToken);
    Task<Result> EnqueueChangeEmail(User user, string callbackUrl, CancellationToken cancellationToken);
    Task<Result> EnqueueAcceptSeller(Event @event, string email, string name, CancellationToken cancellationToken);
    Task<Result> EnqueueDenySeller(Event @event, string email, string name, CancellationToken cancellationToken);
}
