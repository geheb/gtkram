namespace GtKram.Application.UseCases.User.Models;

public record struct UserOtp(bool IsEnabled, string SecretKey, string AuthUri);
