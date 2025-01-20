namespace GtKram.Infrastructure.Persistence.Entities;

public sealed class EmailQueue
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedOn {  get; set; }
    public string? Recipient { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public byte[]? AttachmentBlob { get; set; }
    public string? AttachmentName { get; set; }
    public string? AttachmentMimeType { get; set; }
}
