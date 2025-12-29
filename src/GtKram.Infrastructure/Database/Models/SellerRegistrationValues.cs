namespace GtKram.Infrastructure.Database.Models;

internal sealed class SellerRegistrationValues
{
    public Guid EventId { get; set; }

    public Guid? SellerId { get; set; }

    public string? Email { get; set; }

    public string? Name { get; set; }

    public string? Phone { get; set; }

    public string? Clothing { get; set; }

    public bool? IsAccepted { get; set; }

    public int PreferredType { get; set; }
}
