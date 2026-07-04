namespace Rafiq.Application.Common.Interfaces
{
    public interface INotificationsService
    {
        Task SendSMSAsync(
            string phoneNumber,
            string message,
            CancellationToken cancellationToken);
    }
}
