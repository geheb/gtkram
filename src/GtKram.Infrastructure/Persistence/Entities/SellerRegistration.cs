using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GtKram.Infrastructure.Persistence.Entities;

[Table("seller_registrations")]
internal sealed class SellerRegistration : IEntity
{
    [JsonConverter(typeof(GuidJsonConverter))]
    public Guid Id { get; set; }

    [JsonConverter(typeof(GuidJsonConverter))]
    public Guid? EventId { get; set; }

    public string? Email { get; set; }

    public string? Name { get; set; }

    public string? Phone { get; set; }

    public string? Clothing { get; set; }

    public bool? Accepted { get; set; }

    public int PreferredType { get; set; }

    [JsonConverter(typeof(GuidJsonConverter))]
    public Guid? SellerId { get; set; }

    [JsonIgnore]
    public int Version { get; set; }

    [JsonIgnore]
    public DateTimeOffset? Created { get; set; }
}
