using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Auth.DTOs;
using Rafiq.Domain.Exceptions;

namespace Rafiq.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, ApiResponse<RegisterResponseDto>>
{
    private readonly IIdentityService _identityService;

    public RegisterCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<ApiResponse<RegisterResponseDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await _identityService.PhoneNumberExistsAsync(request.PhoneNumber, cancellationToken))
        {
            throw new ConflictException("An account with this phone number already exists.");
        }

        var user = await _identityService.RegisterAsync(
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneNumber,
            request.Password,
            request.Role,
            cancellationToken);

        return ApiResponse<RegisterResponseDto>.SuccessResponse(user, "Registration successful.");
    }
}
