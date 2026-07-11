using Rafiq.Application.Common.Interfaces;
using System.Security.Cryptography;

namespace Rafiq.Infrastructure.Services.Notifications
{
    public sealed class OtpGenerator : IOtpGenerator
    {
        public string Generate()
        {
            return RandomNumberGenerator
                .GetInt32(100000, 999999)
                .ToString();
        }
    }
}
