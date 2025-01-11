using GtKram.Core.Converter;
using GtKram.Core.Entities;
using System.Globalization;

namespace GtKram.Core.Models.Account;

public class UserDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public bool IsEmailConfirmed { get; }
    public DateTimeOffset? LastLogin { get; }
    public string[] Roles { get; set; } = [];
    public DateTimeOffset? IsLockedUntil { get; set; }
    public bool IsTwoFactorEnabled { get; set; }

    public UserDto()
    {
    }

    internal UserDto(IdentityUserGuid entity, IdnMapping idn, GermanDateTimeConverter dc)
    {
        idn ??= new IdnMapping();

        Id = entity.Id;
        Name = entity.Name;

        var email = entity.Email!.Split('@');
        Email = email[0] + "@" + idn.GetUnicode(email[1]);

        IsEmailConfirmed = entity.EmailConfirmed;
        LastLogin = entity.LastLogin.HasValue ? dc.ToLocal(entity.LastLogin.Value) : null;
        Roles = entity.UserRoles?.Select(e => e.Role!.Name!).ToArray() ?? [];
        var isLocked = entity.LockoutEnabled && entity.LockoutEnd.HasValue && entity.LockoutEnd.Value > DateTimeOffset.UtcNow;
        if (isLocked)
        {
            IsLockedUntil = dc.ToLocal(entity.LockoutEnd!.Value);
        }
        IsTwoFactorEnabled = entity.TwoFactorEnabled;
    }
}
