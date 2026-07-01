using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Auth.DTOs;

namespace Rafiq.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(string Email, string Password, string? DeviceInfo = null, string? IpAddress = null)
    : IRequest<ApiResponse<AuthResponseDto>>;
