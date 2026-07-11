using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Auth.DTOs;

namespace Rafiq.Application.Features.Auth.Queries
{
    public sealed record GetMyAccountQuery : IRequest<ApiResponse<AccountDto>>;
}
