using Microsoft.Extensions.Options;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace Rafiq.Infrastructure.Services
{
    public class NotificationsService(IOptions<TwilioSettings> TwilioOptions) : INotificationsService
    {
        private readonly TwilioSettings twilioSettings = TwilioOptions.Value;

        public Task SendSMSAsync(string phoneNumber, string message, CancellationToken cancellationToken)
        {
            TwilioClient.Init(twilioSettings.AccountSID, twilioSettings.AuthToken);

            var result = MessageResource.CreateAsync(
                body: message,
                from: new Twilio.Types.PhoneNumber(twilioSettings.TwilioPhoneNumber),
                to: phoneNumber);
            return result;
        }
    }
}
