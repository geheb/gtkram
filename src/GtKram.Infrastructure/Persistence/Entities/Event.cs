using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GtKram.Infrastructure.Persistence.Entities;

[Table("events")]
internal sealed class Event : IEntity
{
    [JsonConverter(typeof(GuidJsonConverter))]
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset Start { get; set; }

    public DateTimeOffset End { get; set; }

    public string? Address { get; set; }

    public int MaxSellers { get; set; }

    public int Commission { get; set; }

    public DateTimeOffset RegisterStart { get; set; }

    public DateTimeOffset RegisterEnd { get; set; }

    public DateTimeOffset? EditArticleEnd { get; set; }

    public DateTimeOffset? PickUpLabelsStart{ get; set; }

    public DateTimeOffset? PickUpLabelsEnd { get; set; }

    public bool HasRegistrationsLocked { get; set; }

    [JsonIgnore]
    public int Version {  get; set; }

    [JsonIgnore]
    public DateTimeOffset? Created { get; set; }
}
