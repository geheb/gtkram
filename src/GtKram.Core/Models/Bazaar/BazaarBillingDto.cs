using GtKram.Core.Converter;
using GtKram.Core.Entities;

namespace GtKram.Core.Models.Bazaar;

public class BazaarBillingDto
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public BillingStatus Status { get; set; }
    public Guid UserId { get; set; }
    public string? User { get; set; }
    public decimal Total { get; set; }
    public int ArticleCount { get; set; }

    internal BazaarBillingDto(BazaarBilling entity, int articleCount, GermanDateTimeConverter dc)
    {
        Id = entity.Id;
        CreatedOn = dc.ToLocal(entity.CreatedOn);
        Status = (BillingStatus)entity.Status;
        UserId = entity.UserId!.Value;
        User = entity.User?.Name;
        Total = entity.Total;
        ArticleCount = articleCount;
    }
}
