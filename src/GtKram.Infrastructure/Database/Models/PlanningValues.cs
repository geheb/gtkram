namespace GtKram.Infrastructure.Database.Models;

internal sealed class PlanningValues
{
    public Guid EventId { get; set; }
    public string Name { get; set; } = null!;
    public DateTimeOffset Date { get; set; }
    public TimeOnly From { get; set; }
    public TimeOnly To { get; set; }
    public ICollection<Guid> IdentityIds { get; set; } = [];
    public ICollection<string> Persons { get; set; } = [];
}
