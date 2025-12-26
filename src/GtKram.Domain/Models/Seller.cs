namespace GtKram.Domain.Models;

public sealed class Seller
{
    public Guid Id { get; set; }

    public DateTimeOffset Created { get; set; }

    public Guid EventId { get; set; }

    public Guid IdentityId { get; set; }

    public int SellerNumber { get; set; }

    public SellerRole Role { get; set; }

    public bool CanCheckout { get; set; }

    public int MaxArticleCount { get; set; }
}
