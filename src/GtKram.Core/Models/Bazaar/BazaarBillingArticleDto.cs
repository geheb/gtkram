namespace GtKram.Core.Models.Bazaar;

public sealed class BazaarBillingArticleDto
{
    public Guid Id { get; set; }
    public Guid ArticleId { get; set; }
    public DateTimeOffset AddedOn { get; set; }
    public int SellerNumber { get; set; }
    public string? Name { get; set; }
    public int LabelNumber { get; set; }
    public decimal Price { get; set; }

    public string FormatAsInfo()
    {
        return $"{Name} #{LabelNumber} für {Price:0.00} € (Verkäufernummer {SellerNumber})";
    }

    public string FormatAsAdded()
    {
        return $"{Name} #{LabelNumber} für {Price:0.00} € (Verkäufernummer {SellerNumber}) wurde angelegt.";
    }

    public string FormatAsExists()
    {
        return $"{Name} #{LabelNumber} für {Price:0.00} € (Verkäufernummer {SellerNumber}) wurde bereits erfasst.";
    }
}
