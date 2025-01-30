namespace GtKram.Domain.Models;

public sealed class BazaarSeller
{
    public Guid Id { get; set; }
    public Guid? BazaarEventId { get; set; }
    public Guid? UserId { get; set; }
    public int SellerNumber { get; set; }
    public SellerRole Role { get; set; }
    public int MaxArticleCount { get; set; }
    public bool CanCreateBillings { get; set; }
}
