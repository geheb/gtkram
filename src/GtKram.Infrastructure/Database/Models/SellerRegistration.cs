using GtKram.Infrastructure.Database.Repositories;
using System.Text.Json;

namespace GtKram.Infrastructure.Database.Models;

[JsonTable(TableNames.SellerRegistrations, MapColumns = [nameof(EventId), nameof(SellerId)])]
internal sealed class SellerRegistration : IEntity
{
    public Guid Id { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Updated { get; set; }

    public string JsonProperties { get; set; } = null!;

    public int JsonVersion { get; set; }

    public Guid EventId => Json.EventId;

    public Guid? SellerId => Json.SellerId;

    public SellerRegistrationValues Json { get; set; } = null!;

    public void Deserialize() =>
        Json = JsonSerializer.Deserialize<SellerRegistrationValues>(JsonProperties)!;

    public void Serialize() =>
        JsonProperties = JsonSerializer.Serialize(Json);
}
