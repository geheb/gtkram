using GtKram.Application.Converter;
using GtKram.Infrastructure.Persistence.Entities;

namespace GtKram.Infrastructure.Repositories.Mappings;

internal static class BazaarMapping
{
    public static Domain.Models.BazaarEvent MapToDomain(this BazaarEvent entity, GermanDateTimeConverter dc) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        StartsOn = dc.ToLocal(entity.StartDate),
        EndsOn = dc.ToLocal(entity.EndDate),
        Address = entity.Address,
        MaxSellers = entity.MaxSellers,
        RegisterStartsOn = dc.ToLocal(entity.RegisterStartDate),
        RegisterEndsOn = dc.ToLocal(entity.RegisterEndDate),
        EditArticleEndsOn = entity.EditArticleEndDate.HasValue ? dc.ToLocal(entity.EditArticleEndDate.Value) : null,
        PickUpLabelsStartsOn = entity.PickUpLabelsStartDate.HasValue ? dc.ToLocal(entity.PickUpLabelsStartDate.Value) : null,
        PickUpLabelsEndsOn = entity.PickUpLabelsEndDate.HasValue ? dc.ToLocal(entity.PickUpLabelsEndDate.Value) : null,
        IsRegistrationsLocked = entity.IsRegistrationsLocked,
    };

    public static BazaarEvent MapToEntity(this Domain.Models.BazaarEvent model, BazaarEvent entity, GermanDateTimeConverter dc)
    {
        entity.Name = model.Name;
        entity.Description = model.Description;
        entity.StartDate = model.StartsOn.ToUniversalTime();
        entity.EndDate = model.EndsOn.ToUniversalTime();
        entity.Address = model.Address;
        entity.MaxSellers = model.MaxSellers;
        entity.RegisterStartDate = model.RegisterStartsOn.ToUniversalTime();
        entity.RegisterEndDate = model.RegisterEndsOn.ToUniversalTime();
        entity.EditArticleEndDate = model.EditArticleEndsOn?.ToUniversalTime();
        entity.PickUpLabelsStartDate = model.PickUpLabelsStartsOn?.ToUniversalTime();
        entity.PickUpLabelsEndDate = model.PickUpLabelsEndsOn?.ToUniversalTime();
        entity.IsRegistrationsLocked = model.IsRegistrationsLocked;
        return entity;
    }

    public static Domain.Models.BazaarSellerRegistration MapToDomain(this BazaarSellerRegistration entity, GermanDateTimeConverter dc) => new()
    {
        Id = entity.Id,
        BazaarEventId = entity.BazaarEventId!.Value,
        Email = entity.Email!,
        Name = entity.Name!,
        Phone = entity.Phone!,
        ClothingType = entity.Clothing?.Split(';').Select(c => int.Parse(c)).ToArray(),
        Accepted = entity.Accepted,
        PreferredType = (Domain.Models.SellerRegistrationPreferredType)entity.PreferredType,
        BazaarSellerId = entity.BazaarSellerId
    };

    public static BazaarSellerRegistration MapToEntity(this Domain.Models.BazaarSellerRegistration model, BazaarSellerRegistration entity, GermanDateTimeConverter dc)
    {
        entity.BazaarEventId = model.BazaarEventId;
        entity.Email = model.Email;
        entity.Name = model.Name;
        entity.Phone = model.Phone;
        entity.Clothing = model.ClothingType is not null ? string.Join(';', model.ClothingType) : null;
        entity.Accepted = model.Accepted;
        entity.PreferredType = (int)model.PreferredType;
        entity.BazaarSellerId = model.BazaarSellerId;
        return entity;
    }

    public static Domain.Models.BazaarBilling MapToDomain(this BazaarBilling entity, GermanDateTimeConverter dc) => new()
    {
        Id = entity.Id,
        CreatedOn = dc.ToLocal(entity.CreatedOn),
        Status = (Domain.Models.BillingStatus)entity.Status,
        BazaarEventId = entity.BazaarEventId!.Value,
        UserId = entity.UserId!.Value,
    };

    public static BazaarBilling MapToEntity(this Domain.Models.BazaarBilling model, BazaarBilling entity)
    {
        entity.Status = (int)model.Status;
        entity.BazaarEventId = model.BazaarEventId;
        entity.UserId = model.UserId;
        return entity;
    }

    public static Domain.Models.BazaarBillingArticle MapToDomain(this BazaarBillingArticle entity, GermanDateTimeConverter dc) => new()
    {
        Id = entity.Id,
        CreatedOn = dc.ToLocal(entity.CreatedOn),
        BazaarBillingId = entity.BazaarBillingId!.Value,
        BazaarSellerArticleId = entity.BazaarSellerArticleId!.Value,
    };

    public static Domain.Models.BazaarSellerArticle MapToDomain(this BazaarSellerArticle entity) => new()
    {
        Id = entity.Id,
        BazaarSellerId = entity.BazaarSellerId!.Value,
        LabelNumber = entity.LabelNumber,
        Name = entity.Name,
        Size = entity.Size,
        Price = entity.Price,
        Status = (Domain.Models.SellerArticleStatus)entity.Status,
    };

    public static BazaarSellerArticle MapToEntity(this Domain.Models.BazaarSellerArticle model, BazaarSellerArticle entity)
    {
        entity.BazaarSellerId = model.BazaarSellerId;
        entity.LabelNumber = model.LabelNumber;
        entity.Name = model.Name;
        entity.Size = model.Size;
        entity.Price = model.Price;
        entity.Status = (int)model.Status;
        return entity;
    }

    public static Domain.Models.BazaarSeller MapToDomain(this BazaarSeller entity) => new()
    {
        Id = entity.Id,
        UserId = entity.UserId!.Value,
        CreatedOn = entity.CreatedOn,
        BazaarEventId = entity.BazaarEventId!.Value,
        SellerNumber = entity.SellerNumber,
        Role = (Domain.Models.SellerRole)entity.Role,
        MaxArticleCount = entity.MaxArticleCount,
        CanCreateBillings = entity.CanCreateBillings,
    };

    public static BazaarSeller MapToEntity(this Domain.Models.BazaarSeller model, BazaarSeller entity)
    {
        entity.BazaarEventId = model.BazaarEventId;
        entity.SellerNumber = model.SellerNumber;
        entity.Role = (int)model.Role;
        entity.MaxArticleCount = model.MaxArticleCount;
        entity.CanCreateBillings = model.CanCreateBillings;
        return entity;
    }
}
