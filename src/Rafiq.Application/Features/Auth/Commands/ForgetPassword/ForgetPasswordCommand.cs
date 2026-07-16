using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.Auth.Commands.ForgetPassword
{
    public sealed record ForgotPasswordCommand(string Email) : IRequest<ApiResponseBase>;
}
