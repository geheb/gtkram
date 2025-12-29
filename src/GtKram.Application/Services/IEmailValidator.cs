namespace GtKram.Application.Services;

public interface IEmailValidator
{
    Task<bool> Validate(string email, CancellationToken cancellationToken);
}
