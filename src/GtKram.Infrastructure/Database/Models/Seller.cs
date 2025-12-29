using GtKram.Infrastructure.Database.Repositories;
using System.Text.Json;

namespace GtKram.Infrastructure.Database.Models;

[JsonTable(TableNames.Sellers, MapColumns = [nameof(EventId), nameof(IdentityId), nameof(SellerNumber)])]
internal sealed class Seller : IEntity
{
    public Guid Id { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Updated { get; set; }

    public string JsonProperties { get; set; } = null!;

    public int JsonVersion { get; set; }

    public Guid EventId => Json.EventId;

    public Guid IdentityId => Json.IdentityId;

    public int SellerNumber => Json.SellerNumber;

    public SellerValues Json { get; set; } = null!;

    public void Deserialize() =>
        Json = JsonSerializer.Deserialize<SellerValues>(JsonProperties)!;

    public void Serialize() =>
        JsonProperties = JsonSerializer.Serialize(Json);
}
