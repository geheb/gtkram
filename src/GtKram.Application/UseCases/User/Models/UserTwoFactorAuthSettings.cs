namespace GtKram.Application.UseCases.User.Models;

public sealed record UserTwoFactorAuthSettings(bool IsEnabled, string SecretKey, string AuthUri);
