using GtKram.Application.Converter;
using GtKram.Infrastructure.Database;
using GtKram.Infrastructure.Database.Entities;

namespace GtKram.Infrastructure.Repositories.Mappings;

internal static class BazaarMapping
{
    public static Domain.Models.Event MapToDomain(this Event entity, GermanDateTimeConverter dc) => new()
    {
        Id = entity.Id,
        Name = entity.Name!,
        Description = entity.Description,
        Start = dc.ToLocal(entity.Start),
        End = dc.ToLocal(entity.End),
        Address = entity.Address,
        MaxSellers = entity.MaxSellers,
        Commission = entity.Commission,
        RegisterStart = dc.ToLocal(entity.RegisterStart),
        RegisterEnd = dc.ToLocal(entity.RegisterEnd),
        EditArticleEnd = entity.EditArticleEnd.HasValue ? dc.ToLocal(entity.EditArticleEnd.Value) : null,
        PickUpLabelsStart = entity.PickUpLabelsStart.HasValue ? dc.ToLocal(entity.PickUpLabelsStart.Value) : null,
        PickUpLabelsEnd = entity.PickUpLabelsEnd.HasValue ? dc.ToLocal(entity.PickUpLabelsEnd.Value) : null,
        HasRegistrationsLocked = entity.HasRegistrationsLocked
    };

    public static Event MapToEntity(this Domain.Models.Event model, Event entity, GermanDateTimeConverter dc)
    {
        entity.Name = model.Name;
        entity.Description = model.Description;
        entity.Start = model.Start.ToUniversalTime();
        entity.End = model.End.ToUniversalTime();
        entity.Address = model.Address;
        entity.MaxSellers = model.MaxSellers;
        entity.RegisterStart= model.RegisterStart.ToUniversalTime();
        entity.RegisterEnd = model.RegisterEnd.ToUniversalTime();
        entity.EditArticleEnd = model.EditArticleEnd?.ToUniversalTime();
        entity.PickUpLabelsStart = model.PickUpLabelsStart?.ToUniversalTime();
        entity.PickUpLabelsEnd = model.PickUpLabelsEnd?.ToUniversalTime();
        entity.HasRegistrationsLocked = model.HasRegistrationsLocked;
        return entity;
    }

    public static Domain.Models.SellerRegistration MapToDomain(this SellerRegistration entity, GermanDateTimeConverter dc) => new()
    {
        Id = entity.Id,
        EventId = entity.EventId!.Value,
        Email = entity.Email!,
        Name = entity.Name!,
        Phone = entity.Phone!,
        ClothingType = entity.Clothing?.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(c => int.Parse(c)).ToArray(),
        Accepted = entity.Accepted,
        PreferredType = (Domain.Models.SellerRegistrationPreferredType)entity.PreferredType,
        SellerId = entity.SellerId
    };

    public static SellerRegistration MapToEntity(this Domain.Models.SellerRegistration model, SellerRegistration entity, GermanDateTimeConverter dc)
    {
        entity.EventId = model.EventId;
        entity.Email = model.Email;
        entity.Name = model.Name;
        entity.Phone = model.Phone;
        entity.Clothing = model.ClothingType is not null ? string.Join(';', model.ClothingType) : null;
        entity.Accepted = model.Accepted;
        entity.PreferredType = (int)model.PreferredType;
        entity.SellerId = model.SellerId;
        return entity;
    }

    public static Domain.Models.Checkout MapToDomain(this Entity<Checkout> entity, GermanDateTimeConverter dc) => new()
    {
        Id = entity.Id,
        Created = dc.ToLocal(entity.Created),
        Status = (Domain.Models.CheckoutStatus)entity.Item.Status,
        EventId = entity.Item.EventId!.Value,
        UserId = entity.Item.UserId!.Value,
        ArticleIds = [.. entity.Item.ArticleIds]
    };

    public static Checkout MapToEntity(this Domain.Models.Checkout model, Checkout entity)
    {
        entity.Status = (int)model.Status;
        entity.EventId = model.EventId;
        entity.UserId = model.UserId;
        entity.ArticleIds = [.. model.ArticleIds];
        return entity;
    }

    public static Domain.Models.Article MapToDomain(this Article entity) => new()
    {
        Id = entity.Id,
        SellerId = entity.SellerId!.Value,
        LabelNumber = entity.LabelNumber,
        Name = entity.Name,
        Size = entity.Size,
        Price = entity.Price
    };

    public static Article MapToEntity(this Domain.Models.Article model, Article entity)
    {
        entity.SellerId = model.SellerId;
        entity.LabelNumber = model.LabelNumber;
        entity.Name = model.Name;
        entity.Size = model.Size;
        entity.Price = model.Price;
        return entity;
    }

    public static Domain.Models.Seller MapToDomain(this Entity<Seller> entity, GermanDateTimeConverter dc) => new()
    {
        Id = entity.Id,
        UserId = entity.Item.UserId!.Value,
        Created = dc.ToLocal(entity.Created),
        EventId = entity.Item.EventId!.Value,
        SellerNumber = entity.Item.SellerNumber,
        Role = (Domain.Models.SellerRole)entity.Item.Role,
        MaxArticleCount = entity.Item.MaxArticleCount,
        CanCheckout = entity.Item.CanCheckout,
    };

    public static Seller MapToEntity(this Domain.Models.Seller model, Seller entity)
    {
        entity.EventId = model.EventId;
        entity.SellerNumber = model.SellerNumber;
        entity.Role = (int)model.Role;
        entity.MaxArticleCount = model.MaxArticleCount;
        entity.CanCheckout = model.CanCheckout;
        return entity;
    }
}
