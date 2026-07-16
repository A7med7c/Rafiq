using MediatR;
using Microsoft.AspNetCore.Http;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Auth.DTOs;

namespace Rafiq.Application.Features.Auth.Commands.Register;

public record RegisterCommand(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string Password,
    string ConfirmPassword,
    IFormFile? ProfileImage = null) : IRequest<ApiResponse<RegisterResponseDto>>;
