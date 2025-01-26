using GtKram.Application.Converter;
using GtKram.Application.UseCases.User.Models;
using GtKram.Domain.Models;
using GtKram.Infrastructure.Persistence.Entities;

namespace GtKram.Infrastructure.Repositories.Mappings;

internal static class UserMapping
{
    public static Domain.Models.User MapToDomain(this IdentityUserGuid entity, GermanDateTimeConverter dc) => new()
    {
        Id = entity.Id,
        Name = entity.Name!,
        Email = entity.Email!,
        Roles = [.. entity.UserRoles!.Select(r => r.Role!.Name!.MapToRole())],
        IsEmailConfirmed = entity.EmailConfirmed,
        LastLoginDate = entity.LastLogin is not null ? dc.ToLocal(entity.LastLogin!.Value) : null,
        LockoutEndDate = entity.LockoutEnabled && entity.LockoutEnd is not null ? dc.ToLocal(entity.LockoutEnd!.Value) : null,
        IsTwoFactorEnabled = entity.TwoFactorEnabled
    };

    public static string MapToRole(this UserRoleType role) => role switch
    {
        UserRoleType.Administrator => Roles.Admin,
        UserRoleType.Manager => Roles.Manager,
        UserRoleType.Seller => Roles.Seller,
        UserRoleType.Billing => Roles.Billing,
        _ => throw new NotImplementedException()
    };

    public static UserRoleType MapToRole(this string role) => role switch
    {
        Roles.Admin => UserRoleType.Administrator,
        Roles.Manager => UserRoleType.Manager,
        Roles.Seller => UserRoleType.Seller,
        Roles.Billing => UserRoleType.Billing,
        _ => throw new NotImplementedException()
    };
}
