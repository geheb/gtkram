namespace GtKram.Domain.Models;

public sealed class BazaarBilling
{
    public Guid Id { get; set; }
    public BillingStatus Status { get; set; }
    public Guid BazaarEventId { get; set; }
    public Guid UserId { get; set; }

    public bool IsCompleted => Status == BillingStatus.Completed;
}
