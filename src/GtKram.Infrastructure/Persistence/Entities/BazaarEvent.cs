namespace GtKram.Infrastructure.Persistence.Entities;

internal sealed class BazaarEvent
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset? UpdatedOn { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public string? Address { get; set; }
    public int MaxSellers { get; set; }
    public int Commission { get; set; }
    public DateTimeOffset RegisterStartDate { get; set; }
    public DateTimeOffset RegisterEndDate { get; set; }
    public DateTimeOffset? EditArticleEndDate { get; set; }
    public DateTimeOffset? PickUpLabelsStartDate { get; set; }
    public DateTimeOffset? PickUpLabelsEndDate { get; set; }
    public bool IsRegistrationsLocked { get; set; }

    public ICollection<BazaarSellerRegistration>? SellerRegistrations { get; set; }
    public ICollection<BazaarSeller>?BazaarSellers { get; set; }
    public ICollection<BazaarBilling>? BazaarBillings { get; set; }
}
