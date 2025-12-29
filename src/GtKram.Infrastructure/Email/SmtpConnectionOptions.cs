namespace GtKram.Infrastructure.Email;

internal sealed class SmtpConnectionOptions
{
    public required string Server { get; set; }
    public int Port { get; set; }
    public required string SenderEmail { get; set; }
    public required string SenderName { get; set; }
    public required string LoginName { get; set; }
    public required string LoginPassword { get; set; }
}
