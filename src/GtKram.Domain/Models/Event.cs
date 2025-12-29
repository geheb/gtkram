namespace GtKram.Domain.Models;

public sealed class Event
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public required DateTimeOffset Start { get; set; }

    public required DateTimeOffset End { get; set; }

    public string? Address { get; set; }

    public required int MaxSellers { get; set; }

    public int Commission { get; set; }

    public required DateTimeOffset RegisterStart { get; set; }

    public required DateTimeOffset RegisterEnd { get; set; }

    public required DateTimeOffset? EditArticleEnd { get; set; }

    public required DateTimeOffset? PickUpLabelsStart { get; set; }

    public required DateTimeOffset? PickUpLabelsEnd { get; set; }

    public bool HasRegistrationsLocked { get; set; }
}
