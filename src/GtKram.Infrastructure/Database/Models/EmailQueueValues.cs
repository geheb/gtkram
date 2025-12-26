namespace GtKram.Infrastructure.Database.Models;

internal sealed class EmailQueueValues
{
    public DateTimeOffset? Sent { get; set; }

    public string? Recipient { get; set; }

    public string? Subject { get; set; }

    public string? Body { get; set; }

    public string? AttachmentName { get; set; }

    public string? AttachmentMimeType { get; set; }

    public byte[]? AttachmentBlob { get; set; }
}
