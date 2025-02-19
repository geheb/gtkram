using System.ComponentModel;

namespace GtKram.Domain.Models;

public sealed class BazaarEvent
{
    [ReadOnly(true)]
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset StartsOn { get; set; }

    public DateTimeOffset EndsOn { get; set; }

    public string? Address { get; set; }

    public int MaxSellers { get; set; }

    public int Commission { get; set; }

    public DateTimeOffset RegisterStartsOn { get; set; }

    public DateTimeOffset RegisterEndsOn { get; set; }

    public DateTimeOffset? EditArticleEndsOn { get; set; }

    public DateTimeOffset? PickUpLabelsStartsOn { get; set; }

    public DateTimeOffset? PickUpLabelsEndsOn { get; set; }

    public bool IsRegistrationsLocked { get; set; }
}
