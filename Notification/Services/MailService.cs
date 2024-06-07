using MailKit.Net.Smtp;
using MimeKit;
using Notification.Models;

namespace Notification.Services;

internal class MailService(MailSettings mailSettings)
{
    private readonly MailSettings mailSettings = mailSettings;

    public async Task<string> SendEmailAsync(MailRequest mailRequest)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(mailSettings.DisplayName, mailSettings.Mail));
        email.To.Add(MailboxAddress.Parse(mailRequest.ToEmail));
        email.Subject = mailRequest.Subject;
        var builder = new BodyBuilder();
        builder.HtmlBody = mailRequest.Body;
        email.Body = builder.ToMessageBody();
        using var smtp = new SmtpClient();
        smtp.Connect(mailSettings.Host, mailSettings.Port, MailKit.Security.SecureSocketOptions.StartTls);
        smtp.Authenticate(mailSettings.Username, mailSettings.Password);
        string sendResult = await smtp.SendAsync(email);
        smtp.Disconnect(true);
        return sendResult;
    }
}