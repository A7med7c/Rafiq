using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Auth.DTOs;
using System.Security.Authentication;

namespace Rafiq.Application.Features.Auth.Queries
{
    public sealed class GetMyAccountQueryHandler(ICurrentUserService _currentUserService,
        IIdentityService _identityService) : IRequestHandler<GetMyAccountQuery, ApiResponse<AccountDto>>
    {
        public async Task<ApiResponse<AccountDto>> Handle(GetMyAccountQuery request, CancellationToken cancellationToken)
        {
            if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
                throw new AuthenticationException("User is not authenticated.");

            var account = await _identityService.GetAccountAsync(_currentUserService.UserId.Value, cancellationToken);
            return ApiResponse<AccountDto>.SuccessResponse(account);
        }
    }
}
