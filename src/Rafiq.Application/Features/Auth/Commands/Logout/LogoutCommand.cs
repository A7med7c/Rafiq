using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.Auth.Commands.Logout
{
    public sealed record LogoutCommand(
    string RefreshToken,
    string? IpAddress)
    : IRequest<ApiResponseBase>;
}
