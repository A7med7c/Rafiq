using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.Auth.Commands.PhoneNumber
{
    public sealed record VerifyPhoneCommand(
     string Email,
     string Code,
     OtpPurpose Purpose)
     : IRequest<ApiResponseBase>;
}
