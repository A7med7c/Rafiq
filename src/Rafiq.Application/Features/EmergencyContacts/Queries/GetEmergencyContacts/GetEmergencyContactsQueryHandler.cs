using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.EmergencyContacts.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.EmergencyContacts.Queries.GetEmergencyContacts;

public sealed class GetEmergencyContactsQueryHandler(
    ICurrentUserService currentUserService,
    IEmergencyContactRepository emergencyContactRepository)
    : IRequestHandler<GetEmergencyContactsQuery, ApiResponse<IReadOnlyList<EmergencyContactResponseDto>>>
{
    public async Task<ApiResponse<IReadOnlyList<EmergencyContactResponseDto>>> Handle(
        GetEmergencyContactsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("User is not authenticated.");

        var contacts = await emergencyContactRepository.GetAllByUserIdAsync(userId, cancellationToken);

        var dtos = contacts
            .Select(c => new EmergencyContactResponseDto(
                c.Id,
                c.UserId,
                c.Name,
                c.PhoneNumber,
                c.Relation))
            .ToList();

        return ApiResponse<IReadOnlyList<EmergencyContactResponseDto>>.SuccessResponse(dtos, "Emergency contacts retrieved successfully.");
    }
}
