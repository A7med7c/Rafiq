using Rafiq.Domain.Enums;

namespace Rafiq.Application.Common.Interfaces
{
    public interface IOtpService
    {
        Task SendOtpAsync(Guid userId, string phoneNumber, OtpPurpose purpose, CancellationToken cancellationToken);

        Task<bool> VerifyOtpAsync(Guid userId, string otp, OtpPurpose purpose, CancellationToken cancellationToken);
    }
}
