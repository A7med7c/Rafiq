using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Auth.Commands.RevokeToken;

public sealed class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, ApiResponse<object>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<object>> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId ?? throw new UnauthorizedException("Authentication is required.");
        var token = await _refreshTokenRepository.GetByTokenHashAsync(_tokenService.HashRefreshToken(request.RefreshToken), cancellationToken);

        if (token is null || token.UserId != currentUserId)
        {
            throw new ValidationException(new[] { "The provided refresh token is invalid or does not belong to the authenticated user." });
        }

        if (!token.IsRevoked)
        {
            token.Revoke(request.IpAddress);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ApiResponse<object>.SuccessResponse(null, "Token revoked successfully.");
    }
}
