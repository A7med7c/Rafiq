using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.Auth.Commands.PhoneNumber;

public sealed record ResendPhoneCodeCommand(
    string PhoneNumber)
    : IRequest<ApiResponseBase>;