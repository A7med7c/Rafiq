using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Auth.DTOs;

namespace Rafiq.Application.Features.Auth.Commands.ResetPassword
{
    public sealed record VerifyResetPasswordCommand(string phoneNumber, string code) : IRequest<ApiResponse<VerifyResetOtpResponseDto>>;
}