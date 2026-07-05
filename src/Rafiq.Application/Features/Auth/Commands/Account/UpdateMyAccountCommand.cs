using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Auth.DTOs;

namespace Rafiq.Application.Features.Auth.Commands.Account
{
    public sealed record UpdateMyAccountCommand(
    string FirstName,
    string LastName,
    string PhoneNumber)
    : IRequest<ApiResponse<AccountDto>>;
}
