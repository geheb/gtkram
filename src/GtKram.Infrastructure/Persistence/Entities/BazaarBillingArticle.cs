namespace GtKram.Infrastructure.Persistence.Entities;

internal sealed class BazaarBillingArticle
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid? BazaarBillingId { get; set; }
    public BazaarBilling? BazaarBilling { get; set; }
    public Guid? BazaarSellerArticleId { get; set; }
    public BazaarSellerArticle? BazaarSellerArticle { get; set; }
}
