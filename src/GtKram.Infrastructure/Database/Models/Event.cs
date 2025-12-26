using GtKram.Infrastructure.Database.Repositories;
using System.Text.Json;

namespace GtKram.Infrastructure.Database.Models;

[JsonTable(TableNames.Events)]
internal sealed class Event : IEntity
{
    public Guid Id { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Updated { get; set; }

    public string JsonProperties { get; set; } = null!;

    public int JsonVersion { get; set; }

    public EventValues Json { get; set; } = null!;

    public void Deserialize() =>
        Json = JsonSerializer.Deserialize<EventValues>(JsonProperties)!;

    public void Serialize() =>
        JsonProperties = JsonSerializer.Serialize(Json);
}
