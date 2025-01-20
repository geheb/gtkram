namespace GtKram.Application.Services;

public interface IEmailValidatorService
{
    Task<bool> Validate(string email, CancellationToken cancellationToken);
}
