namespace GtKram.Infrastructure.Persistence.Entities;

internal sealed class BazaarBilling
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset? UpdatedOn { get; set; }
    public int Status { get; set; }
    public Guid? BazaarEventId { get; set; }
    public BazaarEvent? BazaarEvent { get; set; }
    public Guid? UserId { get; set; }
    public IdentityUserGuid? User { get; set; }
    public decimal Total { get; set; }

    public ICollection<BazaarBillingArticle>? BazaarBillingArticles { get; set; }
}
