using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GtKram.Infrastructure.Persistence.Entities;

[Table("sellers")]
internal sealed class Seller : IEntity
{
    public Guid Id { get; set; }

    public string? EventId { get; set; }

    public string? UserId { get; set; }

    public int SellerNumber { get; set; }

    public int Role { get; set; }

    public int MaxArticleCount { get; set; }

    public bool CanCheckout { get; set; }

    [JsonIgnore]
    public int Version { get; set; }
}
