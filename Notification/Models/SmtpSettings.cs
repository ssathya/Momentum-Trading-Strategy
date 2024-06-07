namespace Notification.Models;

public class SmtpSettings
{
    public string? IAMUserName { get; set; }
    public string? SMTPUserName { get; set; }
    public string? SMTPPassword { get; set; }
    public string? DisplayName { get; set; }
    public string? SenderAddress { get; set; }
    public string? ToAddress { get; set; }
    public string? HostServer { get; set; }
}