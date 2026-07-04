using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Auth.DTOs;

namespace Rafiq.Application.Features.Auth.Commands.ExternalLogin
{
    public sealed record GoogleLoginCommand(string IdToken) : IRequest<ApiResponse<AuthResponseDto>>;
}
