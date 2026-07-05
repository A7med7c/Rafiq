using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Auth.DTOs;
using System.Security.Authentication;

namespace Rafiq.Application.Features.Auth.Commands.Account
{
    public sealed class UpdateMyAccountCommandHandler(
     ICurrentUserService _currentUserService, IIdentityService _identityService)
        : IRequestHandler<UpdateMyAccountCommand, ApiResponse<AccountDto>>
    {
        public async Task<ApiResponse<AccountDto>> Handle(UpdateMyAccountCommand request, CancellationToken cancellationToken)
        {
            if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
                throw new AuthenticationException("User is not authenticated.");

            var dto = await _identityService.UpdateAccountAsync(_currentUserService.UserId.Value, request.FirstName, request.LastName,
                                                                            request.PhoneNumber, cancellationToken);

            return ApiResponse<AccountDto>.SuccessResponse(dto, "Account updated successfully.");
        }
    }
}
