using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.Auth.Commands.Account
{
    public sealed record DeleteMyAccountCommand() : IRequest<ApiResponse<string>>;
}
