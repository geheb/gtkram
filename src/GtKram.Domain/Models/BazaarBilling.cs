using System.ComponentModel;

namespace GtKram.Domain.Models;

public sealed class BazaarBilling
{
    [ReadOnly(true)]
    public Guid Id { get; set; }

    [ReadOnly(true)]
    public DateTimeOffset CreatedOn { get; set; }

    public BillingStatus Status { get; set; }

    public Guid BazaarEventId { get; set; }

    public Guid UserId { get; set; }

    public bool IsCompleted => Status == BillingStatus.Completed;
}
