namespace GtKram.Domain.Models;

public sealed class BazaarSellerRegistration
{
    public Guid Id { get; set; }
    public Guid? BazaarEventId { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Clothing { get; set; }
    public bool? Accepted { get; set; }
    public SellerRegistrationPreferredType PreferredType { get; set; }
    public Guid? BazaarSellerId { get; set; }
}
