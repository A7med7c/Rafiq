using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Auth.DTOs;

namespace Rafiq.Application.Features.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Email,
    string PhoneNumber,
    string Password,
    string ConfirmPassword,
    string Role) : IRequest<ApiResponse<RegisterResponseDto>>;
