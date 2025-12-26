namespace GtKram.Infrastructure.Database.Models;

internal sealed class ArticleValues
{
    public Guid SellerId { get; set; }

    public int LabelNumber { get; set; }

    public string? Name { get; set; }

    public string? Size { get; set; }

    public decimal Price { get; set; }
}
