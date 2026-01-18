using GtKram.Infrastructure.Database.Repositories;
using System.Text.Json;

namespace GtKram.Infrastructure.Database.Models;

[JsonTable(TableNames.Plannings, MapColumns = [nameof(EventId)])]
internal sealed class Planning : IEntity
{
    public Guid Id { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Updated { get; set; }

    public string JsonProperties { get; set; } = null!;

    public int JsonVersion { get; set; }

    public PlanningValues Json { get; set; } = null!;

    public Guid EventId => Json.EventId;

    public void Deserialize() =>
        Json = JsonSerializer.Deserialize<PlanningValues>(JsonProperties)!;

    public void Serialize() =>
        JsonProperties = JsonSerializer.Serialize(Json);
}
