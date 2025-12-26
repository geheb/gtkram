using GtKram.Infrastructure.Database.Repositories;
using System.Text.Json;

namespace GtKram.Infrastructure.Database.Models;

[JsonTable(TableNames.EmailQueues, MapColumns = [nameof(IsSent)])]
internal sealed class EmailQueue : IEntity
{
    public Guid Id { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Updated { get; set; }

    public string JsonProperties { get; set; } = null!;

    public int JsonVersion { get; set; }

    public bool IsSent => Json.Sent.HasValue;

    public EmailQueueValues Json { get; set; } = null!;

    public void Deserialize() =>
        Json = JsonSerializer.Deserialize<EmailQueueValues>(JsonProperties)!;

    public void Serialize() =>
        JsonProperties = JsonSerializer.Serialize(Json);
}
