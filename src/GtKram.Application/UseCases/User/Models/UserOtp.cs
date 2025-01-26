namespace GtKram.Application.UseCases.User.Models;

public sealed record UserOtp(bool IsEnabled, string SecretKey, string AuthUri);
