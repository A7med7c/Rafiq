using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.EmergencyContacts.DTOs;
using Rafiq.Domain.Entities.User;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.EmergencyContacts.Commands.CreateEmergencyContact;

public sealed class CreateEmergencyContactCommandHandler(
    ICurrentUserService currentUserService,
    IEmergencyContactRepository emergencyContactRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateEmergencyContactCommand, ApiResponse<EmergencyContactResponseDto>>
{
    public async Task<ApiResponse<EmergencyContactResponseDto>> Handle(
        CreateEmergencyContactCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("User is not authenticated.");

        var emergencyContact = new EmergencyContact(
            userId,
            request.Name.Trim(),
            request.PhoneNumber.Trim(),
            request.Relation.Trim());

        await emergencyContactRepository.AddAsync(emergencyContact, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new EmergencyContactResponseDto(
            emergencyContact.Id,
            emergencyContact.UserId,
            emergencyContact.Name,
            emergencyContact.PhoneNumber,
            emergencyContact.Relation);

        return ApiResponse<EmergencyContactResponseDto>.SuccessResponse(dto, "Emergency contact added successfully.");
    }
}
