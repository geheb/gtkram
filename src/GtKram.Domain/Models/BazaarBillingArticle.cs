using System.ComponentModel;

namespace GtKram.Domain.Models;

public sealed class BazaarBillingArticle
{
    [ReadOnly(true)]
    public Guid Id { get; set; }

    [ReadOnly(true)]
    public DateTimeOffset CreatedOn { get; set; }

    public Guid BazaarBillingId { get; set; }

    public Guid BazaarSellerArticleId { get; set; }
}
