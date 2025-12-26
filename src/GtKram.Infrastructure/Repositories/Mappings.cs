using GtKram.Application.Converter;
using GtKram.Application.UseCases.User.Models;
using GtKram.Infrastructure.Database.Models;
using System.Security.Claims;

namespace GtKram.Infrastructure.Repositories;

internal static class Mappings
{
    public static Domain.Models.User MapToDomain(this Database.Models.Identity entity, DateTimeOffset now, GermanDateTimeConverter dc) => 
        new()
        {
            Id = entity.Id,
            Name = entity.Json.Name!,
            Email = entity.Email!,
            Roles = [.. entity.Json.Claims.Where(c => c.Type == ClaimsIdentity.DefaultRoleClaimType).Select(c => c.Value.MapToRole())],
            IsEmailConfirmed = entity.Json.IsEmailConfirmed,
            LastLoginDate = entity.Json.LastLogin is not null ? dc.ToLocal(entity.Json.LastLogin!.Value) : null,
            LockoutEndDate =
                now < entity.Json.LockoutEnd
                ? dc.ToLocal(entity.Json.LockoutEnd.Value)
                : null,
            IsTwoFactorEnabled = entity.Json.Claims.Contains(Database.Models.IdentityClaim.TwoFactorClaim)
        };

    public static string MapToRole(this Domain.Models.UserRoleType role) => 
        role switch
        {
            Domain.Models.UserRoleType.Administrator => Roles.Admin,
            Domain.Models.UserRoleType.Manager => Roles.Manager,
            Domain.Models.UserRoleType.Seller => Roles.Seller,
            Domain.Models.UserRoleType.Checkout => Roles.Checkout,
            _ => throw new NotImplementedException()
        };

    public static Domain.Models.UserRoleType MapToRole(this string role) => 
        role switch
        {
            Roles.Admin => Domain.Models.UserRoleType.Administrator,
            Roles.Manager => Domain.Models.UserRoleType.Manager,
            Roles.Seller => Domain.Models.UserRoleType.Seller,
            Roles.Checkout => Domain.Models.UserRoleType.Checkout,
            _ => throw new NotImplementedException()
        };

    public static Domain.Models.EmailQueue MapToDomain(this Database.Models.EmailQueue entity) => 
        new()
        {
            Id = entity.Id,
            Recipient = entity.Json.Recipient!,
            Subject = entity.Json.Subject!,
            Body = entity.Json.Body!,
            AttachmentName = entity.Json.AttachmentName,
            AttachmentMimeType = entity.Json.AttachmentMimeType,
            AttachmentBlob = entity.Json.AttachmentBlob,
        };

    public static Domain.Models.Event MapToDomain(this Database.Models.Event entity, GermanDateTimeConverter dc) => 
        new()
        {
            Id = entity.Id,
            Name = entity.Json.Name!,
            Description = entity.Json.Description,
            Start = dc.ToLocal(entity.Json.Start),
            End = dc.ToLocal(entity.Json.End),
            Address = entity.Json.Address,
            MaxSellers = entity.Json.MaxSellers,
            Commission = entity.Json.Commission,
            RegisterStart = dc.ToLocal(entity.Json.RegisterStart),
            RegisterEnd = dc.ToLocal(entity.Json.RegisterEnd),
            EditArticleEnd = entity.Json.EditArticleEnd.HasValue ? dc.ToLocal(entity.Json.EditArticleEnd.Value) : null,
            PickUpLabelsStart = entity.Json.PickUpLabelsStart.HasValue ? dc.ToLocal(entity.Json.PickUpLabelsStart.Value) : null,
            PickUpLabelsEnd = entity.Json.PickUpLabelsEnd.HasValue ? dc.ToLocal(entity.Json.PickUpLabelsEnd.Value) : null,
            HasRegistrationsLocked = entity.Json.HasRegistrationsLocked
        };

    public static Database.Models.Event MapToEntity(this Domain.Models.Event model, Database.Models.Event entity, GermanDateTimeConverter dc)
    {
        entity.Json.Name = model.Name;
        entity.Json.Description = model.Description;
        entity.Json.Start = model.Start.ToUniversalTime();
        entity.Json.End = model.End.ToUniversalTime();
        entity.Json.Address = model.Address;
        entity.Json.MaxSellers = model.MaxSellers;
        entity.Json.RegisterStart = model.RegisterStart.ToUniversalTime();
        entity.Json.RegisterEnd = model.RegisterEnd.ToUniversalTime();
        entity.Json.EditArticleEnd = model.EditArticleEnd?.ToUniversalTime();
        entity.Json.PickUpLabelsStart = model.PickUpLabelsStart?.ToUniversalTime();
        entity.Json.PickUpLabelsEnd = model.PickUpLabelsEnd?.ToUniversalTime();
        entity.Json.HasRegistrationsLocked = model.HasRegistrationsLocked;
        return entity;
    }

    public static Domain.Models.SellerRegistration MapToDomain(this Database.Models.SellerRegistration entity, GermanDateTimeConverter dc) => 
        new()
        {
            Id = entity.Id,
            EventId = entity.Json.EventId,
            Email = entity.Json.Email!,
            Name = entity.Json.Name!,
            Phone = entity.Json.Phone!,
            ClothingType = entity.Json.Clothing?.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(c => int.Parse(c)).ToArray(),
            IsAccepted = entity.Json.IsAccepted,
            PreferredType = (Domain.Models.SellerRegistrationPreferredType)entity.Json.PreferredType,
            SellerId = entity.Json.SellerId
        };

    public static Database.Models.SellerRegistration MapToEntity(this Domain.Models.SellerRegistration model, Database.Models.SellerRegistration entity, GermanDateTimeConverter dc)
    {
        entity.Json.EventId = model.EventId;
        entity.Json.Email = model.Email;
        entity.Json.Name = model.Name;
        entity.Json.Phone = model.Phone;
        entity.Json.Clothing = model.ClothingType is not null ? string.Join(';', model.ClothingType) : null;
        entity.Json.IsAccepted = model.IsAccepted;
        entity.Json.PreferredType = (int)model.PreferredType;
        entity.Json.SellerId = model.SellerId;
        return entity;
    }

    public static Domain.Models.Checkout MapToDomain(this Database.Models.Checkout entity, GermanDateTimeConverter dc) => 
        new()
        {
            Id = entity.Id,
            Created = dc.ToLocal(entity.Created),
            Status = (Domain.Models.CheckoutStatus)entity.Json.Status,
            EventId = entity.EventId,
            IdentityId = entity.IdentityId,
            ArticleIds = entity.Json.ArticleIds,
        };

    public static Database.Models.Checkout MapToEntity(this Domain.Models.Checkout model, Checkout entity)
    {
        entity.Json.Status = (int)model.Status;
        entity.Json.EventId = model.EventId;
        entity.Json.IdentityId = model.IdentityId;
        entity.Json.ArticleIds = [.. model.ArticleIds];
        return entity;
    }

    public static Domain.Models.Article MapToDomain(this Database.Models.Article entity) => 
        new()
        {
            Id = entity.Id,
            SellerId = entity.Json.SellerId,
            LabelNumber = entity.Json.LabelNumber,
            Name = entity.Json.Name,
            Size = entity.Json.Size,
            Price = entity.Json.Price
        };

    public static Database.Models.Article MapToEntity(this Domain.Models.Article model, Database.Models.Article entity)
    {
        entity.Json.SellerId = model.SellerId;
        entity.Json.LabelNumber = model.LabelNumber;
        entity.Json.Name = model.Name;
        entity.Json.Size = model.Size;
        entity.Json.Price = model.Price;
        return entity;
    }

    public static Domain.Models.Seller MapToDomain(this Database.Models.Seller entity, GermanDateTimeConverter dc) => 
        new()
        {
            Id = entity.Id,
            Created = dc.ToLocal(entity.Created),
            EventId = entity.EventId,
            IdentityId = entity.IdentityId,
            SellerNumber = entity.Json.SellerNumber,
            Role = (Domain.Models.SellerRole)entity.Json.Role,
            MaxArticleCount = entity.Json.MaxArticleCount,
            CanCheckout = entity.Json.CanCheckout,
        };

    public static Database.Models.Seller MapToEntity(this Domain.Models.Seller model, Database.Models.Seller entity)
    {
        entity.Json.EventId = model.EventId;
        entity.Json.SellerNumber = model.SellerNumber;
        entity.Json.Role = (int)model.Role;
        entity.Json.MaxArticleCount = model.MaxArticleCount;
        entity.Json.CanCheckout = model.CanCheckout;
        return entity;
    }
}
