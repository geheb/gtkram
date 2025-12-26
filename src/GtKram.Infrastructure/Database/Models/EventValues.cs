namespace GtKram.Infrastructure.Database.Models;

internal sealed class EventValues
{
    public string? Name { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset Start { get; set; }

    public DateTimeOffset End { get; set; }

    public string? Address { get; set; }

    public int MaxSellers { get; set; }

    public int Commission { get; set; }

    public DateTimeOffset RegisterStart { get; set; }

    public DateTimeOffset RegisterEnd { get; set; }

    public DateTimeOffset? EditArticleEnd { get; set; }

    public DateTimeOffset? PickUpLabelsStart { get; set; }

    public DateTimeOffset? PickUpLabelsEnd { get; set; }

    public bool HasRegistrationsLocked { get; set; }
}
