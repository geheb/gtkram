using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GtKram.Infrastructure.Persistence.Entities;

[Table("checkouts")]
internal sealed class Checkout : IEntity
{
    public Guid Id { get; set; }

    public int Status { get; set; }

    public string? EventId { get; set; }

    public string? UserId { get; set; }

    public string[] ArticleIds { get; set; } = [];

    [JsonIgnore]
    public int Version { get; set; }
}
