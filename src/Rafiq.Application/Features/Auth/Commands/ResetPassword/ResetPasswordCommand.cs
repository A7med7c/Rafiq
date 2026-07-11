using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.Auth.Commands.ResetPassword
{
    public sealed record ResetPasswordCommand(string resetToken, string newPassword) : IRequest<ApiResponseBase>;
}
