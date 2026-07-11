using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using System.Security.Authentication;

namespace Rafiq.Application.Features.Auth.Commands.ChangePassword
{
    public sealed class ChangePasswordCommandHandler(
    IIdentityService identityService,
    ICurrentUserService currentUserService)
    : IRequestHandler<ChangePasswordCommand, ApiResponseBase>
    {
        public async Task<ApiResponseBase> Handle(
            ChangePasswordCommand request,
            CancellationToken cancellationToken)
        {
            if (!currentUserService.IsAuthenticated || currentUserService.UserId is null)
                throw new AuthenticationException("User is not authenticated.");

            await identityService.ChangePasswordAsync(
                currentUserService.UserId.Value,
                request.CurrentPassword,
                request.NewPassword,
                cancellationToken);

            return ApiResponseBase.SuccessResponse("Password changed successfully.");
        }
    }
}
