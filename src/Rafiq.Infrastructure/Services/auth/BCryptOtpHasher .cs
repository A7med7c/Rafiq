using Rafiq.Application.Common.Interfaces;


namespace Rafiq.Infrastructure.Services.auth
{
    internal class BCryptOtpHasher : IOtpHasher
    {
        public string Hash(string otp)
        => BCrypt.Net.BCrypt.HashPassword(otp);

        public bool Verify(string otp, string hash)
            => BCrypt.Net.BCrypt.Verify(otp, hash);
    }
}
