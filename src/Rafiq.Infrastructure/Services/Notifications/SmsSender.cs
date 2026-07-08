using Microsoft.Extensions.Options;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace Rafiq.Infrastructure.Services.Notifications
{
    public class NotificationsService(IOptions<TwilioSettings> twilioOptions) : INotificationsService
    {
        private readonly TwilioSettings _twilioSettings = twilioOptions.Value;

        public Task SendSMSAsync(string phoneNumber, string message, CancellationToken cancellationToken)
        {
            TwilioClient.Init(_twilioSettings.AccountSID, _twilioSettings.AuthToken);
            return MessageResource.CreateAsync(
                body: message,
                from: new Twilio.Types.PhoneNumber(_twilioSettings.TwilioPhoneNumber),
                to: new Twilio.Types.PhoneNumber(NormalizePhoneNumber(phoneNumber)));
        }

        private static string NormalizePhoneNumber(string phoneNumber)
        {
            phoneNumber = phoneNumber.Trim();

            // Already in Egyptian international format
            if (phoneNumber.StartsWith("+20"))
                return phoneNumber;

            // Starts with 20 but missing +
            if (phoneNumber.StartsWith("20"))
                return $"+{phoneNumber}";

            // Local Egyptian number (e.g. 01012345678)
            if (phoneNumber.StartsWith("0"))
                return $"+2{phoneNumber}";

            // Fallback
            return $"+2{phoneNumber}";
        }

        private static string FormatPhoneNumberForTwilio(string phoneNumber)
        {
            if (phoneNumber.StartsWith('+'))
            {
                return phoneNumber;
            }

            return $"+2{phoneNumber}";
        }
    }
}