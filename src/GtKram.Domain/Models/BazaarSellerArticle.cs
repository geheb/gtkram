namespace GtKram.Domain.Models;

public sealed class BazaarSellerArticle
{
    public Guid Id { get; set; }
    public Guid? BazaarSellerId { get; set; }
    public int LabelNumber { get; set; }
    public string? Name { get; set; }
    public string? Size { get; set; }
    public decimal Price { get; set; }
    public SellerArticleStatus Status { get; set; }
}
