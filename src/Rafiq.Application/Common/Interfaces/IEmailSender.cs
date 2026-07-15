namespace Rafiq.Application.Common.Interfaces
{
    public interface IEmailSender
    {
        Task SendEmailAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken);
    }
}
