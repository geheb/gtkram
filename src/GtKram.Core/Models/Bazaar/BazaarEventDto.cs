using GtKram.Core.Converter;
using GtKram.Core.Entities;
using GtKram.Core.Extensions;

namespace GtKram.Core.Models.Bazaar;

public class BazaarEventDto
{
    public Guid? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public string? Address { get; set; }
    public int MaxSellers { get; set; }
    public DateTimeOffset RegisterStartDate { get; set; }
    public DateTimeOffset RegisterEndDate { get; set; }
    public DateTimeOffset? EditArticleEndDate { get; set; }
    public bool IsRegistrationsLocked { get; set; }
    public bool IsBillingExpired { get; set; }
    public int SellerRegistrationCount { get; set; }
    public bool CanRegister { get; set; }
    public int BillingCount { get; set; }
    public decimal SoldTotal { get; set; }
    public decimal CommissionTotal { get; set; }
    public DateTimeOffset? PickUpLabelsStartDate { get; set; }
    public DateTimeOffset? PickUpLabelsEndDate { get; set; }

    public string FormatEvent(GermanDateTimeConverter dc)
    {
        var nameAndDescription = Name + (string.IsNullOrEmpty(Description) ? string.Empty : (" - " + Description));
        return nameAndDescription + ", " + dc.Format(StartDate, EndDate);
    }

    public BazaarEventDto()
    {

    }

    internal BazaarEventDto(BazaarEvent entity, int sellerRegistrationCount, int billingCount, decimal soldTotal, GermanDateTimeConverter dc)
    {
        Id = entity.Id;
        Name = entity.Name;
        Description = entity.Description;
        StartDate = dc.ToLocal(entity.StartDate);
        EndDate = dc.ToLocal(entity.EndDate);
        Address = entity.Address;
        MaxSellers = entity.MaxSellers;
        RegisterStartDate = dc.ToLocal(entity.RegisterStartDate);
        RegisterEndDate = dc.ToLocal(entity.RegisterEndDate);
        EditArticleEndDate = entity.EditArticleEndDate.HasValue ? dc.ToLocal(entity.EditArticleEndDate.Value) : null;
        IsBillingExpired = DateTimeOffset.UtcNow.Date > entity.EndDate.Date;
        SellerRegistrationCount = sellerRegistrationCount;
        PickUpLabelsStartDate = entity.PickUpLabelsStartDate.HasValue ? dc.ToLocal(entity.PickUpLabelsStartDate.Value) : null;
        PickUpLabelsEndDate = entity.PickUpLabelsEndDate.HasValue ? dc.ToLocal(entity.PickUpLabelsEndDate.Value) : null;
        IsRegistrationsLocked = entity.IsRegistrationsLocked;

        CanRegister =
            !IsRegistrationsLocked &&
            DateTimeOffset.UtcNow >= entity.RegisterStartDate &&
            DateTimeOffset.UtcNow <= entity.RegisterEndDate &&
            sellerRegistrationCount < MaxSellers;

        BillingCount = billingCount;
        SoldTotal = soldTotal;
        CommissionTotal = (entity.Commission / 100.0M) * soldTotal;
    }

    internal bool To(BazaarEvent entity)
    {
        if (Id.HasValue) entity.Id = Id.Value;
        var count = 0;
        if (entity.SetValue(e => e.Name, Name)) count++;
        if (entity.SetValue(e => e.Description, Description)) count++;
        if (entity.SetValue(e => e.StartDate, StartDate)) count++;
        if (entity.SetValue(e => e.EndDate, EndDate)) count++;
        if (entity.SetValue(e => e.Address, Address)) count++;
        if (entity.SetValue(e => e.MaxSellers, MaxSellers)) count++;
        if (entity.SetValue(e => e.RegisterStartDate, RegisterStartDate)) count++;
        if (entity.SetValue(e => e.RegisterEndDate, RegisterEndDate)) count++;
        if (entity.SetValue(e => e.EditArticleEndDate, EditArticleEndDate)) count++;
        if (entity.SetValue(e => e.PickUpLabelsStartDate, PickUpLabelsStartDate)) count++;
        if (entity.SetValue(e => e.PickUpLabelsEndDate, PickUpLabelsEndDate)) count++;
        if (entity.SetValue(e => e.IsRegistrationsLocked, IsRegistrationsLocked)) count++;
        return count > 0;
    }
}
