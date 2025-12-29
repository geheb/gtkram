namespace GtKram.Infrastructure.Database.Models;

internal sealed class SellerValues
{
    public Guid EventId { get; set; }

    public Guid IdentityId { get; set; }

    public int SellerNumber { get; set; }

    public int Role { get; set; }

    public int MaxArticleCount { get; set; }

    public bool CanCheckout { get; set; }
}
