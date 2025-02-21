using System.ComponentModel;

namespace GtKram.Domain.Models;

public sealed class BazaarEvent
{
    [ReadOnly(true)]
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public required DateTimeOffset StartsOn { get; set; }

    public required DateTimeOffset EndsOn { get; set; }

    public string? Address { get; set; }

    public required int MaxSellers { get; set; }

    [ReadOnly(true)]
    public int Commission { get; set; }

    public required DateTimeOffset RegisterStartsOn { get; set; }

    public required DateTimeOffset RegisterEndsOn { get; set; }

    public required DateTimeOffset? EditArticleEndsOn { get; set; }

    public required DateTimeOffset? PickUpLabelsStartsOn { get; set; }

    public required DateTimeOffset? PickUpLabelsEndsOn { get; set; }

    public bool IsRegistrationsLocked { get; set; }
}
