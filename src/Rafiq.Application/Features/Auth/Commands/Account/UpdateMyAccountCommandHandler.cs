using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Auth.DTOs;
using Rafiq.Domain.Exceptions;

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

            var userId = _currentUserService.UserId.Value;

            var existingByPhone = await _identityService.GetByPhoneAsync(request.PhoneNumber, cancellationToken);
            if (existingByPhone is not null && existingByPhone.UserId != userId)
                throw new ConflictException("An account with this phone number already exists.");

            var dto = await _identityService.UpdateAccountAsync(userId, request.FirstName, request.LastName,
                                                                request.PhoneNumber, cancellationToken);

            return ApiResponse<AccountDto>.SuccessResponse(dto, "Account updated successfully.");
        }
    }
}
