namespace GtKram.Domain.Models;

public sealed class BazaarSeller
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid BazaarEventId { get; set; }
    public int SellerNumber { get; set; }
    public SellerRole Role { get; set; }
    public bool CanCreateBillings { get; set; }
    public int MaxArticleCount { get; set; }
}
