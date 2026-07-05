using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Auth.Commands.ResetPassword
{
    public class ResetPasswordCommandHandler(IIdentityService _identityService, IResetTokenService _resetTokenService,
        IRefreshTokenRepository _refreshTokenRepository, IUnitOfWork _unitOfWork)
        : IRequestHandler<ResetPasswordCommand, ApiResponseBase>
    {
        public async Task<ApiResponseBase> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var userId = _resetTokenService.ValidateResetToken(request.resetToken);
            await _identityService.ResetPasswordAsync(userId, request.newPassword, cancellationToken);

            await _refreshTokenRepository.RevokeAllByUserIdAsync(userId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ApiResponseBase.SuccessResponse("Password has been reset successfully.");
        }
    }
}
