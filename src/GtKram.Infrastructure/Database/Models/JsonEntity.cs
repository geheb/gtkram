namespace GtKram.Infrastructure.Database.Models;

public abstract class JsonEntity<T> where T : new()
{
    public Guid Id { get; set; }
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset? Updated { get; set; }
    public int Version { get; set; }
    public string? Data { get; set; }
    public T Value { get; set; } = new();
}