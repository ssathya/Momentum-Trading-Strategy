// See https://aka.ms/new-console-template for more information
using Notification.Models;
using System.Runtime.InteropServices;
using System.Text.Json;

Console.WriteLine("Hello, World!");
string[] arguments = Environment.GetCommandLineArgs();
if (arguments.Length != 2)
{
    Console.WriteLine("Application Usage error");
    Environment.Exit(1);
}
string failedApplicationName = arguments[1];
string envHome = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "HOMEPATH" : "HOME";
string home = Environment.GetEnvironmentVariable(envHome) ?? "";
string keyFilePath = Path.Combine(home, ".keys", "SMTPSettings.json");
if (!File.Exists(keyFilePath))
{
    Console.WriteLine("Could not read key file");
    Console.WriteLine("Exiting!");
    Environment.Exit(1);
}
string json = File.ReadAllText(keyFilePath) ?? "";
SmtpSettings? smtpSettings = JsonSerializer.Deserialize<SmtpSettings>(json);
if (smtpSettings == null)
{
    Console.WriteLine("Error Deserializing SMTPSettings.json");
    Environment.Exit(1);
}
var mailSettings = new MailSettings()
{
    Host = smtpSettings.HostServer,
    Port = 587,
    DisplayName = smtpSettings.DisplayName,
    Mail = smtpSettings.SenderAddress,
    Username = smtpSettings.SMTPUserName,
    Password = smtpSettings.SMTPPassword
};
Notification.Services.MailService mailService = new(mailSettings);
string bodyMessage = $"<b>{failedApplicationName}</b> did not execute as expected!" +
    $"\n<br/>Approximate time of job failure is {DateTime.Now.ToString("MM/dd/yyyy hh:mm tt")}";
MailRequest mailRequest = new MailRequest
{
    Body = bodyMessage,
    Subject = "Job failure",
    ToEmail = smtpSettings.ToAddress
};
string sendResult = await mailService.SendEmailAsync(mailRequest);
Console.WriteLine(sendResult);