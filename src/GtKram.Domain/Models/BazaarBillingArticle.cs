namespace GtKram.Domain.Models;

public sealed class BazaarBillingArticle
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid BazaarBillingId { get; set; }
    public Guid BazaarSellerArticleId { get; set; }
}
