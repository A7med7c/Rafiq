using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;

namespace Rafiq.Infrastructure.Services.Notifications
{
    public class EmailSender(IOptions<EmailSettings> emailOptions) : IEmailSender
    {
        private readonly EmailSettings _emailSettings = emailOptions.Value;

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.From));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var client = new SmtpClient();

            await client.ConnectAsync(
                _emailSettings.Host,
                _emailSettings.Port,
                _emailSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                cancellationToken);

            await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password, cancellationToken);

            await client.SendAsync(message, cancellationToken);

            await client.DisconnectAsync(true, cancellationToken);
        }
    }
}
