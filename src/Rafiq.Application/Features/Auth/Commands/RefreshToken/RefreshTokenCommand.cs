using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Auth.DTOs;

namespace Rafiq.Application.Features.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken, string? DeviceInfo = null, string? IpAddress = null)
    : IRequest<ApiResponse<AuthResponseDto>>;
