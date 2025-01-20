namespace GtKram.Application.UseCases.Bazaar.Models;

public sealed class BazaarSellerArticleDto
{
    public Guid? Id { get; set; }
    public int LabelNumber { get; set; }
    public string? Name { get; set; }
    public string? Size { get; set; }
    public decimal Price { get; set; }
    public SellerArticleStatus Status { get; set; } 
    public int SellerNumber { get; set; }
    public bool IsSold => Status == SellerArticleStatus.Sold;
    public bool CanEdit => Status == SellerArticleStatus.Created;
}
