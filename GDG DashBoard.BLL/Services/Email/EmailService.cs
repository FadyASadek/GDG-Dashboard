using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace GDG_DashBoard.BLL.Services.Email;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlContent)
    {
        var emailSettings = _config.GetSection("EmailSettings");
        var smtpServer = emailSettings["SmtpServer"];
        var smtpPort = int.Parse(emailSettings["SmtpPort"]!);
        var smtpUser = emailSettings["SmtpUser"];
        var smtpPass = emailSettings["SmtpPass"];
        var fromEmail = emailSettings["FromEmail"];

        var emailMessage = new MimeMessage();
        emailMessage.From.Add(new MailboxAddress("GDG Dashboard", fromEmail));
        emailMessage.To.Add(new MailboxAddress("", to));
        emailMessage.Subject = subject;
        emailMessage.Body = new TextPart(TextFormat.Html)
        {
            Text = htmlContent
        };

        using (var client = new SmtpClient())
        {
            await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUser, smtpPass);
            await client.SendAsync(emailMessage);
            await client.DisconnectAsync(true);
        }
    }
}
