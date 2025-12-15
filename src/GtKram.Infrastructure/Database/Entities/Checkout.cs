using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GtKram.Infrastructure.Database.Entities;

[Table("checkouts")]
internal sealed class Checkout : IEntity
{
    [JsonConverter(typeof(GuidJsonConverter))]
    public Guid Id { get; set; }

    public int Status { get; set; }

    [JsonConverter(typeof(GuidJsonConverter))]
    public Guid? EventId { get; set; }

    [JsonConverter(typeof(GuidJsonConverter))]
    public Guid? UserId { get; set; }

    [JsonConverter(typeof(GuidJsonArrayConverter))]
    public IEnumerable<Guid> ArticleIds { get; set; } = [];

    [JsonIgnore]
    public int Version { get; set; }

    [JsonIgnore]
    public DateTimeOffset? Created { get; set; }
}
