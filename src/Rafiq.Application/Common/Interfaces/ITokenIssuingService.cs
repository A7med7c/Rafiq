using Rafiq.Application.Features.Auth.DTOs;

namespace Rafiq.Application.Common.Interfaces
{
    public interface ITokenIssuingService
    {
        Task<AuthResponseDto> IssueTokensAsync(IdentityUserDto user, CancellationToken cancellationToken);
    }
}
