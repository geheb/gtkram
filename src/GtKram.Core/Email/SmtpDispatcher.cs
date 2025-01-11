namespace GtKram.Core.Email;

using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

public sealed class SmtpDispatcher
{
    private readonly SmtpConnectionOptions _connectionOptions;

    public SmtpDispatcher(IOptions<SmtpConnectionOptions> options)
    {
        _connectionOptions = options.Value;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage, Attachment? attachment = null)
    {
        var from = new MailAddress(_connectionOptions.SenderEmail, _connectionOptions.SenderName);
        var to = new MailAddress(email);

        using var message = new MailMessage(from, to);
        message.Subject = subject;
        message.Body = htmlMessage;
        message.IsBodyHtml = true;
        message.BodyEncoding = Encoding.UTF8;
        if (attachment is not null)
        {
            message.Attachments.Add(attachment);
        }

        using var client = new SmtpClient(_connectionOptions.Server, _connectionOptions.Port);
        if (!string.IsNullOrEmpty(_connectionOptions.LoginName))
        {
            client.UseDefaultCredentials = false;
            client.EnableSsl = true; // only Explicit SSL supported
            client.Credentials = new NetworkCredential(_connectionOptions.LoginName, _connectionOptions.LoginPassword);
        }

        await client.SendMailAsync(message);
    }
}
