namespace GtKram.Application.UseCases.User.Models;

public sealed record UserTwoFactor(bool IsEnabled, string SecretKey, string AuthUri);
