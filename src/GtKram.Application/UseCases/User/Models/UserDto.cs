namespace GtKram.Application.UseCases.User.Models;

public class UserDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public DateTimeOffset? LastLogin { get; set; }
    public string[] Roles { get; set; } = [];
    public DateTimeOffset? IsLockedUntil { get; set; }
    public bool IsTwoFactorEnabled { get; set; }
}
