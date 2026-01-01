namespace GtKram.Infrastructure.Database.Models;

internal sealed class CheckoutValues
{
    public int Status { get; set; }

    public Guid EventId { get; set; }

    public Guid IdentityId { get; set; }

    public ICollection<Guid> ArticleIds { get; set; } = [];

    public decimal? Total { get; set; }
}
