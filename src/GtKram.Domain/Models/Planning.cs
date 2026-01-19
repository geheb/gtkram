namespace GtKram.Domain.Models;

public sealed class Planning
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string Name { get; set; } = null!;
    public DateTimeOffset Date { get; set; }
    public TimeOnly From { get; set; }
    public TimeOnly To { get; set; }
    public ICollection<Guid> IdentityIds { get; set; } = [];
    public ICollection<string> Persons { get; set; } = [];

    public string BuildHelpers(Dictionary<Guid, string> userMap) =>
        (
            string.Join(", ", IdentityIds.Select(id => userMap.TryGetValue(id, out var u) ? u : id.ToString())) +
            ", " + 
            string.Join(", ", Persons.Select(p => $"{p}*"))
        ).Trim(',', ' ');

    public int HelperCount => IdentityIds.Count + Persons.Count;
}
