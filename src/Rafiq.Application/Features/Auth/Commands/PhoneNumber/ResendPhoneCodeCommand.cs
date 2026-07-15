using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.Auth.Commands.PhoneNumber;

public sealed record ResendPhoneCodeCommand(
    string Email, OtpPurpose Purpose)
    : IRequest<ApiResponseBase>;