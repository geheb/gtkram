using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GtKram.Infrastructure.Persistence.Entities;

[Table("checkouts")]
internal sealed class Checkout : IEntity
{
    public Guid Id { get; set; }

    public int Status { get; set; }

    [JsonConverter(typeof(GuidToChar32Converter))]
    public Guid? EventId { get; set; }

    [JsonConverter(typeof(GuidToChar32Converter))]
    public Guid? UserId { get; set; }

    public Guid[] ArticleIds { get; set; } = [];

    [JsonIgnore]
    public int Version { get; set; }
}
