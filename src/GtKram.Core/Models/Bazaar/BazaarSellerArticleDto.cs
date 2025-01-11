using GtKram.Core.Entities;
using GtKram.Core.Extensions;

namespace GtKram.Core.Models.Bazaar;

public sealed class BazaarSellerArticleDto
{
    public Guid? Id { get; set; }
    public int LabelNumber { get; set; }
    public string? Name { get; set; }
    public string? Size { get; set; }
    public decimal Price { get; set; }
    public SellerArticleStatus Status { get; set; } 
    public int SellerNumber { get; }
    public bool IsSold => Status == SellerArticleStatus.Sold;
    public bool CanEdit => Status == SellerArticleStatus.Created;

    public BazaarSellerArticleDto()
    {
    }

    internal BazaarSellerArticleDto(BazaarSellerArticle entity)
    {
        Id = entity.Id;
        LabelNumber = entity.LabelNumber;
        Name = entity.Name;
        Size = entity.Size;
        Price = entity.Price;
        Status = (SellerArticleStatus)entity.Status;
        SellerNumber = entity.BazaarSeller!.SellerNumber;
    }

    internal bool To(BazaarSellerArticle entity)
    {
        if (Id.HasValue) entity.Id = Id.Value;
        var count = 0;
        if (entity.SetValue(e => e.Name, Name)) count++;
        if (entity.SetValue(e => e.Size, Size)) count++;
        if (entity.SetValue(e => e.Price, Price)) count++;
        return count > 0;
    }
}
