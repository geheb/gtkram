using GtKram.Application.Converter;
using GtKram.Application.UseCases.Bazaar.Models;
using GtKram.Infrastructure.Extensions;
using GtKram.Infrastructure.Persistence.Entities;
using System.Globalization;

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
        entity.Id = model.Id;
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
        entity.Id = model.Id;
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

    public static Domain.Models.BazaarBilling MapToDomain(this BazaarBilling entity) => new()
    {
        Id = entity.Id,
        Status = (Domain.Models.BillingStatus)entity.Status,
        BazaarEventId = entity.BazaarEventId,
        UserId = entity.UserId,
        Total = entity.Total,
    };

    public static BazaarBilling MapToEntity(this Domain.Models.BazaarBilling model, BazaarBilling entity)
    {
        entity.Id = model.Id;
        entity.Status = (int)model.Status;
        entity.BazaarEventId = model.BazaarEventId;
        entity.UserId = model.UserId;
        entity.Total = model.Total;
        return entity;
    }

    public static Domain.Models.BazaarBillingArticle MapToDomain(this BazaarBillingArticle entity, GermanDateTimeConverter dc) => new()
    {
        Id = entity.Id,
        CreatedOn = dc.ToLocal(entity.CreatedOn),
        BazaarBillingId = entity.BazaarBillingId,
        BazaarSellerArticleId = entity.BazaarSellerArticleId,
    };

    public static BazaarBillingArticle MapToEntity(this Domain.Models.BazaarBillingArticle model, BazaarBillingArticle entity)
    {
        entity.Id = model.Id;
        entity.CreatedOn = model.CreatedOn.ToUniversalTime();
        entity.BazaarBillingId = model.BazaarBillingId;
        entity.BazaarSellerArticleId = model.BazaarSellerArticleId;
        return entity;
    }

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
        entity.Id = model.Id;
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
        CreatedOn = entity.CreatedOn,
        BazaarEventId = entity.BazaarEventId!.Value,
        SellerNumber = entity.SellerNumber,
        Role = (Domain.Models.SellerRole)entity.Role,
        MaxArticleCount = entity.MaxArticleCount,
        CanCreateBillings = entity.CanCreateBillings,
    };

    public static BazaarSeller MapToEntity(this Domain.Models.BazaarSeller model, BazaarSeller entity)
    {
        entity.Id = model.Id;
        entity.CreatedOn = model.CreatedOn;
        entity.BazaarEventId = model.BazaarEventId;
        entity.SellerNumber = model.SellerNumber;
        entity.Role = (int)model.Role;
        entity.MaxArticleCount = model.MaxArticleCount;
        entity.CanCreateBillings = model.CanCreateBillings;
        return entity;
    }
















    public static BazaarBillingDto MapToDto(this BazaarBilling entity, int articleCount, GermanDateTimeConverter dc) => new()
    {
        Id = entity.Id,
        CreatedOn = dc.ToLocal(entity.CreatedOn),
        Status = (Domain.Models.BillingStatus)entity.Status,
        UserId = entity.UserId!.Value,
        User = entity.User?.Name,
        Total = entity.Total,
        ArticleCount = articleCount
    };

    public static BazaarEventDto MapToDto(this BazaarEvent entity, int sellerRegistrationCount, int billingCount, decimal soldTotal, GermanDateTimeConverter dc) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        StartDate = dc.ToLocal(entity.StartDate),
        EndDate = dc.ToLocal(entity.EndDate),
        Address = entity.Address,
        MaxSellers = entity.MaxSellers,
        RegisterStartDate = dc.ToLocal(entity.RegisterStartDate),
        RegisterEndDate = dc.ToLocal(entity.RegisterEndDate),
        EditArticleEndDate = entity.EditArticleEndDate.HasValue ? dc.ToLocal(entity.EditArticleEndDate.Value) : null,
        IsBillingExpired = DateTimeOffset.UtcNow.Date > entity.EndDate.Date,
        SellerRegistrationCount = sellerRegistrationCount,
        PickUpLabelsStartDate = entity.PickUpLabelsStartDate.HasValue ? dc.ToLocal(entity.PickUpLabelsStartDate.Value) : null,
        PickUpLabelsEndDate = entity.PickUpLabelsEndDate.HasValue ? dc.ToLocal(entity.PickUpLabelsEndDate.Value) : null,
        IsRegistrationsLocked = entity.IsRegistrationsLocked,

        CanRegister =
            !entity.IsRegistrationsLocked &&
            DateTimeOffset.UtcNow >= entity.RegisterStartDate &&
            DateTimeOffset.UtcNow <= entity.RegisterEndDate &&
            sellerRegistrationCount < entity.MaxSellers,

        BillingCount = billingCount,
        SoldTotal = soldTotal,
        CommissionTotal = (entity.Commission / 100.0M) * soldTotal
    };

    public static bool MapToEntity(this BazaarEventDto dto, BazaarEvent entity)
    {
        if (dto.Id.HasValue) entity.Id = dto.Id.Value;
        var count = 0;
        if (entity.SetValue(e => e.Name, dto.Name)) count++;
        if (entity.SetValue(e => e.Description, dto.Description)) count++;
        if (entity.SetValue(e => e.StartDate, dto.StartDate)) count++;
        if (entity.SetValue(e => e.EndDate, dto.EndDate)) count++;
        if (entity.SetValue(e => e.Address, dto.Address)) count++;
        if (entity.SetValue(e => e.MaxSellers, dto.MaxSellers)) count++;
        if (entity.SetValue(e => e.RegisterStartDate, dto.RegisterStartDate)) count++;
        if (entity.SetValue(e => e.RegisterEndDate, dto.RegisterEndDate)) count++;
        if (entity.SetValue(e => e.EditArticleEndDate, dto.EditArticleEndDate)) count++;
        if (entity.SetValue(e => e.PickUpLabelsStartDate, dto.PickUpLabelsStartDate)) count++;
        if (entity.SetValue(e => e.PickUpLabelsEndDate, dto.PickUpLabelsEndDate)) count++;
        if (entity.SetValue(e => e.IsRegistrationsLocked, dto.IsRegistrationsLocked)) count++;
        return count > 0;
    }

    public static BazaarSellerArticleDto MapToDto(this BazaarSellerArticle entity) => new()
    {
        Id = entity.Id,
        LabelNumber = entity.LabelNumber,
        Name = entity.Name,
        Size = entity.Size,
        Price = entity.Price,
        Status = (Domain.Models.SellerArticleStatus)entity.Status,
        SellerNumber = entity.BazaarSeller!.SellerNumber
    };

    public static bool MapToEntity(this BazaarSellerArticleDto dto, BazaarSellerArticle entity)
    {
        if (dto.Id.HasValue) entity.Id = dto.Id.Value;
        var count = 0;
        if (entity.SetValue(e => e.Name, dto.Name)) count++;
        if (entity.SetValue(e => e.Size, dto.Size)) count++;
        if (entity.SetValue(e => e.Price, dto.Price)) count++;
        return count > 0;
    }

    public static BazaarSellerDto MapToDto(this BazaarSeller entity, int articleCount, GermanDateTimeConverter dc)
    {
        var @event = entity.BazaarEvent!;
        var editArticleEndDateUtc = @event.EditArticleEndDate ?? @event.StartDate;
        return new()
        {
            Id = entity.Id,
            EditArticleEndDate = dc.ToLocal(editArticleEndDateUtc),
            EditArticleExpired = DateTimeOffset.UtcNow > editArticleEndDateUtc,

            EventNameAndDescription = @event.Name + (string.IsNullOrEmpty(@event.Description) ? string.Empty : (" - " + @event.Description)),
            StartDate = dc.ToLocal(@event.StartDate),
            EndDate = dc.ToLocal(@event.EndDate),
            EventAddress = @event.Address,
            Commission = @event.Commission,
            IsEventExpired = DateTimeOffset.UtcNow > @event.EndDate,

            UserId = entity.UserId,
            SellerNumber = entity.SellerNumber,
            Role = (Domain.Models.SellerRole)entity.Role,
            ArticleCount = articleCount,
            MaxArticleCount = entity.MaxArticleCount,

            RegistrationName = entity.BazaarSellerRegistration?.Name,
            RegistrationEmail = entity.BazaarSellerRegistration?.Email,
            RegistrationPhone = entity.BazaarSellerRegistration?.Phone,
            IsRegisterAccepted = entity.BazaarSellerRegistration?.Accepted == true,
            CanCreateBillings = entity.CanCreateBillings
        };
    }

    public static BazaarSellerRegistrationDto MapToDto(this BazaarSellerRegistration entity, int articleCount, IdnMapping idn)
    {
        var email = entity.Email!.Split('@');
        return new()
        {
            Id = entity.Id,
            Name = entity.Name,
            Email = email[0] + "@" + idn.GetUnicode(email[1]),
            Phone = entity.Phone,
            Clothing = entity.Clothing?.Split(';').Select(v => int.Parse(v, CultureInfo.InvariantCulture)).ToArray(),
            Accepted = entity.Accepted,
            BazaarSellerId = entity.BazaarSellerId,
            Role = (Domain.Models.SellerRole?)entity.BazaarSeller?.Role,
            SellerNumber = entity.BazaarSeller?.SellerNumber,
            ArticleCount = entity.BazaarSeller is not null ? articleCount : null,
            HasKita = entity.PreferredType == 1,
            IsEventExpired = entity.BazaarEvent != null && DateTimeOffset.UtcNow > entity.BazaarEvent.EndDate
        };
    }

    public static bool MapToEntity(this BazaarSellerRegistrationDto dto, BazaarSellerRegistration entity)
    {
        if (dto.Id.HasValue) entity.Id = dto.Id.Value;
        var count = 0;
        if (entity.SetValue(e => e.Name, dto.Name)) count++;
        if (entity.SetValue(e => e.Email, dto.Email)) count++;
        if (entity.SetValue(e => e.Phone, dto.Phone)) count++;
        var clothing = dto.Clothing != null && dto.Clothing.Length > 0 ? string.Join(";", dto.Clothing) : null;
        if (entity.SetValue(e => e.Clothing, clothing)) count++;
        if (entity.SetValue(e => e.Accepted, dto.Accepted)) count++;
        if (entity.SetValue(e => e.PreferredType, dto.HasKita ? 1 : 0)) count++;
        return count > 0;
    }
}
