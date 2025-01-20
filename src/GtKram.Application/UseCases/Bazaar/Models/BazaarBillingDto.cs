namespace GtKram.Application.UseCases.Bazaar.Models;

public sealed class BazaarBillingDto
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public BillingStatus Status { get; set; }
    public Guid UserId { get; set; }
    public string? User { get; set; }
    public decimal Total { get; set; }
    public int ArticleCount { get; set; }
}
