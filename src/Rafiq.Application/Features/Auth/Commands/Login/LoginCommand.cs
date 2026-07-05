using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Auth.DTOs;

namespace Rafiq.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(string LoginIdentifier, string Password)
    : IRequest<ApiResponse<AuthResponseDto>>;
