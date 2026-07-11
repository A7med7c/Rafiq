using Rafiq.Application.Features.Auth.DTOs;

namespace Rafiq.Application.Common.Interfaces
{
    public interface IGoogleTokenValidator
    {
        Task<GoogleUserInfoDto> ValidateAsync(string IdToken, CancellationToken cancellationToken);
    }
}
