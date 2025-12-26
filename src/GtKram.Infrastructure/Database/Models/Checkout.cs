using GtKram.Infrastructure.Database.Repositories;
using System.Text.Json;

namespace GtKram.Infrastructure.Database.Models;

[JsonTable(TableNames.Checkouts, MapColumns = [nameof(EventId), nameof(IdentityId)])]
internal sealed class Checkout : IEntity
{
    public Guid Id { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Updated { get; set; }

    public string JsonProperties { get; set; } = null!;

    public int JsonVersion { get; set; }

    public Guid EventId => Json.EventId;

    public Guid IdentityId => Json.IdentityId;

    public CheckoutValues Json { get; set; } = null!;

    public void Deserialize() =>
        Json = JsonSerializer.Deserialize<CheckoutValues>(JsonProperties)!;

    public void Serialize() =>
        JsonProperties = JsonSerializer.Serialize(Json);
}
