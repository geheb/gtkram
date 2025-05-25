namespace GtKram.Domain.Models;

public sealed class Checkout 
{
    public Guid Id { get; set; }

    public DateTimeOffset Created { get; set; }

    public CheckoutStatus Status { get; set; }

    public Guid EventId { get; set; }

    public Guid UserId { get; set; }

    public bool IsCompleted => Status == CheckoutStatus.Completed;

    public ICollection<Guid> ArticleIds { get; set; } = [];
}
