using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Auth.Commands.Logout
{
    public class LogoutCommandHandler(IRefreshTokenRepository _refreshTokenRepository,
    ITokenHasher _tokenHasher,
    IUnitOfWork _unitOfWork) : IRequestHandler<LogoutCommand, ApiResponseBase>
    {
        public async Task<ApiResponseBase> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            var tokenHash = _tokenHasher.Hash(request.RefreshToken);

            var token = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
            if (token is null)
                return ApiResponseBase.SuccessResponse("Logged out successfully.");

            if (token.IsActive)
            {
                token.Revoke();
                _refreshTokenRepository.Update(token);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            return ApiResponseBase.SuccessResponse("Logged out successfully.");
        }



    }
}
