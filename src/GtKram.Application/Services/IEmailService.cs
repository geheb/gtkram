using FluentResults;
using GtKram.Domain.Models;

namespace GtKram.Application.Services;

public interface IEmailService
{
    Task<Result> EnqueueConfirmRegistration(string email, string name, string callbackUrl, CancellationToken cancellationToken);
    Task<Result> EnqueueResetPassword(string email, string name, string callbackUrl, CancellationToken cancellationToken);
    Task<Result> EnqueueChangeEmail(string email, string name, string callbackUrl, CancellationToken cancellationToken);
    Task<Result> EnqueueAcceptSeller(BazaarEvent @event, string email, string name, CancellationToken cancellationToken);
    Task<Result> EnqueueDenySeller(BazaarEvent @event, string email, string name, CancellationToken cancellationToken);
}
