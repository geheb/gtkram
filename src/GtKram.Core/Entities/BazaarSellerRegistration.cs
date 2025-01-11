namespace GtKram.Core.Entities;

internal sealed class BazaarSellerRegistration : ChangedOn
{
    public Guid Id { get; set; }
    public Guid? BazaarEventId { get; set; }
    public BazaarEvent? BazaarEvent { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Clothing { get; set; }
    public bool? Accepted { get; set; }
    public int PreferredType { get; set; }
    public Guid? BazaarSellerId { get; set; }
    public BazaarSeller? BazaarSeller { get; set; }
}
