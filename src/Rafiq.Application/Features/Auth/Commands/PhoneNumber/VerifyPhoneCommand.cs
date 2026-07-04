using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.Auth.Commands.PhoneNumber
{
    public sealed record VerifyPhoneCommand(
     string PhoneNumber,
     string Code)
     : IRequest<ApiResponseBase>;
}
