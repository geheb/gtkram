using GtKram.Application.Converter;
using GtKram.Application.UseCases.User.Models;
using GtKram.Domain.Models;
using GtKram.Infrastructure.Database.Entities;
using System.Security.Claims;

namespace GtKram.Infrastructure.Repositories.Mappings;

internal static class UserMapping
{
    public static Domain.Models.User MapToDomain(this Identity entity, DateTimeOffset now, GermanDateTimeConverter dc) => new()
    {
        Id = entity.Id,
        Name = entity.Name!,
        Email = entity.Email!,
        Roles = [.. entity.Claims.Where(c => c.Type == ClaimsIdentity.DefaultRoleClaimType).Select(c => c.Value.MapToRole())],
        IsEmailConfirmed = entity.IsEmailConfirmed,
        LastLoginDate = entity.LastLogin is not null ? dc.ToLocal(entity.LastLogin!.Value) : null,
        LockoutEndDate =
            now < entity.LockoutEnd
            ? dc.ToLocal(entity.LockoutEnd.Value)
            : null,
        IsTwoFactorEnabled = entity.Claims.Contains(IdentityClaim.TwoFactorClaim)
    };

    public static string MapToRole(this UserRoleType role) => role switch
    {
        UserRoleType.Administrator => Roles.Admin,
        UserRoleType.Manager => Roles.Manager,
        UserRoleType.Seller => Roles.Seller,
        UserRoleType.Checkout => Roles.Checkout,
        _ => throw new NotImplementedException()
    };

    public static UserRoleType MapToRole(this string role) => role switch
    {
        Roles.Admin => UserRoleType.Administrator,
        Roles.Manager => UserRoleType.Manager,
        Roles.Seller => UserRoleType.Seller,
        Roles.Checkout => UserRoleType.Checkout,
        _ => throw new NotImplementedException()
    };
}
