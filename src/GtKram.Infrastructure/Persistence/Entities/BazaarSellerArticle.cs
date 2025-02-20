namespace GtKram.Infrastructure.Persistence.Entities;

internal sealed class BazaarSellerArticle
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset? UpdatedOn { get; set; }
    public Guid? BazaarSellerId { get; set; }
    public BazaarSeller? BazaarSeller { get; set; }
    public int LabelNumber { get; set; }
    public string? Name { get; set; }
    public string? Size { get; set; }
    public decimal Price { get; set; }

    public BazaarBillingArticle? BazaarBillingArticle { get; set; }
}
