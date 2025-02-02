namespace GtKram.Domain.Models;

public sealed class BazaarSellerRegistration
{
    public Guid Id { get; set; }
    public required Guid BazaarEventId { get; set; }
    public required string Email { get; set; }
    public required string Name { get; set; }
    public required string Phone { get; set; }
    public int[]? ClothingType { get; set; }
    public bool? Accepted { get; set; }
    public SellerRegistrationPreferredType PreferredType { get; set; }
    public Guid? BazaarSellerId { get; set; }
}
