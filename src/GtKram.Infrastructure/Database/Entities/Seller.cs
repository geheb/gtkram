using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GtKram.Infrastructure.Database.Entities;

[Table("sellers")]
internal sealed class Seller : IEntity
{
    [JsonConverter(typeof(GuidJsonConverter))]
    public Guid Id { get; set; }

    [JsonConverter(typeof(GuidJsonConverter))]
    public Guid? EventId { get; set; }

    [JsonConverter(typeof(GuidJsonConverter))]
    public Guid? UserId { get; set; }

    public int SellerNumber { get; set; }

    public int Role { get; set; }

    public int MaxArticleCount { get; set; }

    public bool CanCheckout { get; set; }

    [JsonIgnore]
    public int Version { get; set; }

    [JsonIgnore]
    public DateTimeOffset? Created { get; set; }
}
