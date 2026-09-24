using ErrorOr;
using GtKram.Application.Converter;
using GtKram.Application.UseCases.User.Models;
using GtKram.Infrastructure.Database;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace GtKram.Infrastructure.Repositories;

internal static class Mappings
{
    public static Domain.Models.User MapToDomain(this Database.Models.Identity entity, DateTimeOffset now, GermanDateTimeConverter dc) => 
        new()
        {
            Id = entity.Id,
            Name = entity.Value.Name,
            Email = entity.Value.Email,
            Roles = [.. entity.Value.Claims.Where(c => c.Type == ClaimsIdentity.DefaultRoleClaimType).Select(c => c.Value.MapToRole())],
            IsEmailConfirmed = entity.Value.IsEmailConfirmed,
            LastLoginDate = entity.Value.LastLogin is not null ? dc.ToLocal(entity.Value.LastLogin!.Value) : null,
            LockoutEndDate =
                now < entity.Value.LockoutEnd
                ? dc.ToLocal(entity.Value.LockoutEnd.Value)
                : null,
            IsTwoFactorEnabled = entity.Value.Claims.Contains(UserClaims.TwoFactorClaim)
        };

    public static string MapToRole(this Domain.Models.UserRoleType role) => 
        role switch
        {
            Domain.Models.UserRoleType.Administrator => Roles.Admin,
            Domain.Models.UserRoleType.Manager => Roles.Manager,
            Domain.Models.UserRoleType.Seller => Roles.Seller,
            Domain.Models.UserRoleType.Checkout => Roles.Checkout,
            Domain.Models.UserRoleType.Helper => Roles.Helper,
            _ => throw new NotImplementedException()
        };

    public static Domain.Models.UserRoleType MapToRole(this string role) => 
        role switch
        {
            Roles.Admin => Domain.Models.UserRoleType.Administrator,
            Roles.Manager => Domain.Models.UserRoleType.Manager,
            Roles.Seller => Domain.Models.UserRoleType.Seller,
            Roles.Checkout => Domain.Models.UserRoleType.Checkout,
            Roles.Helper => Domain.Models.UserRoleType.Helper,
            _ => throw new NotImplementedException()
        };

    public static Domain.Models.EmailQueue MapToDomain(this Database.Models.EmailQueue entity) => 
        new()
        {
            Id = entity.Id,
            Recipient = entity.Value.Recipient!,
            Subject = entity.Value.Subject!,
            Body = entity.Value.Body!,
            AttachmentName = entity.Value.AttachmentName,
            AttachmentMimeType = entity.Value.AttachmentMimeType,
            AttachmentBlob = entity.Value.AttachmentBlob,
        };

    public static Domain.Models.Event MapToDomain(this Database.Models.Event entity, GermanDateTimeConverter dc) => 
        new()
        {
            Id = entity.Id,
            Name = entity.Value.Name!,
            Description = entity.Value.Description,
            Start = dc.ToLocal(entity.Value.Start),
            End = dc.ToLocal(entity.Value.End),
            Address = entity.Value.Address,
            MaxSellers = entity.Value.MaxSellers,
            Commission = entity.Value.Commission,
            RegisterStart = dc.ToLocal(entity.Value.RegisterStart),
            RegisterEnd = dc.ToLocal(entity.Value.RegisterEnd),
            EditArticleEnd = entity.Value.EditArticleEnd.HasValue ? dc.ToLocal(entity.Value.EditArticleEnd.Value) : null,
            PickUpLabelsStart = entity.Value.PickUpLabelsStart.HasValue ? dc.ToLocal(entity.Value.PickUpLabelsStart.Value) : null,
            PickUpLabelsEnd = entity.Value.PickUpLabelsEnd.HasValue ? dc.ToLocal(entity.Value.PickUpLabelsEnd.Value) : null,
            HasRegistrationsLocked = entity.Value.HasRegistrationsLocked
        };

    public static Database.Models.Event MapToEntity(this Domain.Models.Event model, Database.Models.Event entity)
    {
        entity.Value.Name = model.Name;
        entity.Value.Description = model.Description;
        entity.Value.Start = model.Start.ToUniversalTime();
        entity.Value.End = model.End.ToUniversalTime();
        entity.Value.Address = model.Address;
        entity.Value.MaxSellers = model.MaxSellers;
        entity.Value.RegisterStart = model.RegisterStart.ToUniversalTime();
        entity.Value.RegisterEnd = model.RegisterEnd.ToUniversalTime();
        entity.Value.EditArticleEnd = model.EditArticleEnd?.ToUniversalTime();
        entity.Value.PickUpLabelsStart = model.PickUpLabelsStart?.ToUniversalTime();
        entity.Value.PickUpLabelsEnd = model.PickUpLabelsEnd?.ToUniversalTime();
        entity.Value.HasRegistrationsLocked = model.HasRegistrationsLocked;
        return entity;
    }

    public static Domain.Models.SellerRegistration MapToDomain(this Database.Models.SellerRegistration entity) => 
        new()
        {
            Id = entity.Id,
            Updated = entity.Updated,
            EventId = entity.Value.EventId,
            Email = entity.Value.Email!,
            Name = entity.Value.Name!,
            Phone = entity.Value.Phone!,
            ClothingType = entity.Value.Clothing?.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(c => int.Parse(c)).ToArray(),
            IsAccepted = entity.Value.IsAccepted,
            PreferredType = (Domain.Models.SellerRegistrationPreferredType)entity.Value.PreferredType,
            SellerId = entity.Value.SellerId
        };

    public static Database.Models.SellerRegistration MapToEntity(this Domain.Models.SellerRegistration model, Database.Models.SellerRegistration entity, ILookupNormalizer lookupNormalizer)
    {
        entity.Value.EventId = model.EventId;
        entity.Value.Email = model.Email;
        entity.Value.NormalizedEmail = lookupNormalizer.NormalizeEmail(model.Email);
        entity.Value.Name = model.Name;
        entity.Value.Phone = model.Phone;
        entity.Value.Clothing = model.ClothingType is not null ? string.Join(';', model.ClothingType) : null;
        entity.Value.IsAccepted = model.IsAccepted;
        entity.Value.PreferredType = (int)model.PreferredType;
        entity.Value.SellerId = model.SellerId;
        return entity;
    }

    public static Domain.Models.Checkout MapToDomain(this Database.Models.Checkout entity, GermanDateTimeConverter dc) => 
        new()
        {
            Id = entity.Id,
            Created = dc.ToLocal(entity.Created),
            Status = (Domain.Models.CheckoutStatus)entity.Value.Status,
            EventId = entity.Value.EventId,
            IdentityId = entity.Value.IdentityId,
            ArticleIds = entity.Value.ArticleIds,
            Total = entity.Value.Total ?? 0,
        };

    public static Database.Models.Checkout MapToEntity(this Domain.Models.Checkout model, Database.Models.Checkout entity)
    {
        entity.Value.Status = (int)model.Status;
        entity.Value.EventId = model.EventId;
        entity.Value.IdentityId = model.IdentityId;
        entity.Value.ArticleIds = [.. model.ArticleIds];
        entity.Value.Total = model.Total;
        return entity;
    }

    public static Domain.Models.Article MapToDomain(this Database.Models.Article entity) => 
        new()
        {
            Id = entity.Id,
            SellerId = entity.Value.SellerId,
            LabelNumber = entity.Value.LabelNumber,
            Name = entity.Value.Name,
            Size = entity.Value.Size,
            Price = entity.Value.Price
        };

    public static Database.Models.Article MapToEntity(this Domain.Models.Article model, Database.Models.Article entity)
    {
        entity.Value.SellerId = model.SellerId;
        entity.Value.LabelNumber = model.LabelNumber;
        entity.Value.Name = model.Name;
        entity.Value.Size = model.Size;
        entity.Value.Price = model.Price;
        return entity;
    }

    public static Domain.Models.Seller MapToDomain(this Database.Models.Seller entity, GermanDateTimeConverter dc) => 
        new()
        {
            Id = entity.Id,
            Created = dc.ToLocal(entity.Created),
            EventId = entity.Value.EventId,
            IdentityId = entity.Value.IdentityId,
            SellerNumber = entity.Value.SellerNumber,
            Role = (Domain.Models.SellerRole)entity.Value.Role,
            MaxArticleCount = entity.Value.MaxArticleCount,
            CanCheckout = entity.Value.CanCheckout,
        };

    public static Database.Models.Seller MapToEntity(this Domain.Models.Seller model, Database.Models.Seller entity)
    {
        entity.Value.EventId = model.EventId;
        entity.Value.SellerNumber = model.SellerNumber;
        entity.Value.Role = (int)model.Role;
        entity.Value.MaxArticleCount = model.MaxArticleCount;
        entity.Value.CanCheckout = model.CanCheckout;
        return entity;
    }

    public static Domain.Models.Planning MapToDomain(this Database.Models.Planning entity, GermanDateTimeConverter dc) =>
        new()
        {
            Id = entity.Id,
            EventId = entity.Value.EventId,
            Date = dc.ToLocal(entity.Value.Date),
            Name = entity.Value.Name,
            From = entity.Value.From,
            To = entity.Value.To,
            MaxHelper = entity.Value.MaxHelper,
            IdentityIds = [ .. entity.Value.IdentityIds],
            CheckedIdentityIds = [.. entity.Value.CheckedIdentityIds],
            Persons = [.. entity.Value.Persons],
            CheckedPersons = [.. entity.Value.CheckedPersons],
        };

    public static Database.Models.Planning MapToEntity(this Domain.Models.Planning model, Database.Models.Planning entity)
    {
        entity.Id = model.Id;
        entity.Value.EventId = model.EventId;
        entity.Value.Date = model.Date.ToUniversalTime();
        entity.Value.Name = model.Name;
        entity.Value.From = model.From;
        entity.Value.To = model.To;
        entity.Value.MaxHelper = model.MaxHelper;
        entity.Value.IdentityIds = [.. model.IdentityIds];
        entity.Value.CheckedIdentityIds = [.. model.CheckedIdentityIds];
        entity.Value.Persons = [.. model.Persons];
        entity.Value.CheckedPersons = [.. model.CheckedPersons];
        return entity;
    }
}
