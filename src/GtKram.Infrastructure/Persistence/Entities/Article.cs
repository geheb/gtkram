using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GtKram.Infrastructure.Persistence.Entities;

[Table("articles")]
internal sealed class Article : IEntity
{
    [JsonConverter(typeof(GuidJsonConverter))]
    public Guid Id { get; set; }

    [JsonConverter(typeof(GuidJsonConverter))]
    public Guid? SellerId { get; set; }

    public int LabelNumber { get; set; }

    public string? Name { get; set; }

    public string? Size { get; set; }

    public decimal Price { get; set; }

    [JsonIgnore]
    public int Version { get; set; }

    [JsonIgnore]
    public DateTimeOffset? Created { get; set; }
}
