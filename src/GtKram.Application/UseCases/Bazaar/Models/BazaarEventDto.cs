using GtKram.Application.Converter;

namespace GtKram.Application.UseCases.Bazaar.Models;

public sealed class BazaarEventDto
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
        return nameAndDescription + ", " + dc.FormatShort(StartDate, EndDate);
    }
}
