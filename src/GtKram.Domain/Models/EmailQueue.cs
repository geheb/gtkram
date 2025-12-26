namespace GtKram.Domain.Models;

public sealed class EmailQueue
{
    public Guid Id { get; set; }

    public required string Recipient { get; set; }

    public required string Subject { get; set; }

    public required string Body { get; set; }

    public byte[]? AttachmentBlob { get; set; }

    public string? AttachmentName { get; set; }

    public string? AttachmentMimeType { get; set; }
}
