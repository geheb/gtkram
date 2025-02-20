namespace GtKram.Infrastructure.Persistence.Entities;

internal sealed class BazaarSeller
{
    public Guid Id { get; set; }
    public DateTimeOffset? CreatedOn { get; set; }
    public DateTimeOffset? UpdatedOn { get; set; }
    public Guid? BazaarEventId { get; set; }
    public BazaarEvent? BazaarEvent { get; set; }
    public Guid? UserId { get; set; }
    public IdentityUserGuid? User { get; set; }
    public int SellerNumber { get; set; }
    public int Role { get; set; }
    public int MaxArticleCount { get; set; }
    public bool CanCreateBillings { get; set; }

    public BazaarSellerRegistration? BazaarSellerRegistration { get; set; }
    public ICollection<BazaarSellerArticle>? BazaarSellerArticles { get; set; }
}
