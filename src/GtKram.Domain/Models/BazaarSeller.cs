using System.ComponentModel;

namespace GtKram.Domain.Models;

public sealed class BazaarSeller
{
    [ReadOnly(true)]
    public Guid Id { get; set; }

    [ReadOnly(true)]
    public Guid UserId { get; set; }

    [ReadOnly(true)]
    public DateTimeOffset CreatedOn { get; set; }

    public Guid BazaarEventId { get; set; }

    public int SellerNumber { get; set; }

    public SellerRole Role { get; set; }

    public bool CanCreateBillings { get; set; }

    public int MaxArticleCount { get; set; }
}
