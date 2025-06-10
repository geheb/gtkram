using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GtKram.Infrastructure.Persistence.Entities;

[Table("email_queue")]
internal sealed class EmailQueue : IEntity
{
    [JsonConverter(typeof(GuidJsonConverter))]
    public Guid Id { get; set; }

    public DateTimeOffset? Sent { get; set; }

    public string? Recipient { get; set; }

    public string? Subject { get; set; }

    public string? Body { get; set; }

    public byte[]? AttachmentBlob { get; set; }

    public string? AttachmentName { get; set; }

    public string? AttachmentMimeType { get; set; }

    [JsonIgnore]
    public int Version {  get; set; }
}
