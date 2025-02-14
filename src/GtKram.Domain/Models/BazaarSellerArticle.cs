namespace GtKram.Domain.Models;

public sealed class BazaarSellerArticle
{
    public Guid Id { get; set; }
    public Guid BazaarSellerId { get; set; }
    public int LabelNumber { get; set; }
    public string? Name { get; set; }
    public string? Size { get; set; }
    public decimal Price { get; set; }

    //TODO remove 
    public SellerArticleStatus Status { get; set; }
    public bool IsSold => Status == SellerArticleStatus.Sold;
    public bool CanEdit => Status == SellerArticleStatus.Created;
}
