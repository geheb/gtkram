namespace GtKram.Infrastructure.Persistence.Entities;

internal sealed class BazaarBilling : ChangedOn
{
    public Guid Id { get; set; }
    public int Status { get; set; }
    public Guid? BazaarEventId { get; set; }
    public BazaarEvent? BazaarEvent { get; set; }
    public Guid? UserId { get; set; }
    public IdentityUserGuid? User { get; set; }
    public decimal Total { get; set; }
    public ICollection<BazaarBillingArticle>? BazaarBillingArticles { get; set; }
}
