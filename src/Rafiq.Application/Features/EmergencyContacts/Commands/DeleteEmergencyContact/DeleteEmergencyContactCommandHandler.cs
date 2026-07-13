using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.EmergencyContacts.Commands.DeleteEmergencyContact;

public sealed class DeleteEmergencyContactCommandHandler(
    ICurrentUserService currentUserService,
    IEmergencyContactRepository emergencyContactRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteEmergencyContactCommand, ApiResponse<string>>
{
    public async Task<ApiResponse<string>> Handle(
        DeleteEmergencyContactCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("User is not authenticated.");

        var contact = await emergencyContactRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("EmergencyContact", request.Id);

        if (contact.UserId != userId)
        {
            throw new UnauthorizedException("You are not authorized to delete this emergency contact.");
        }

        emergencyContactRepository.Delete(contact);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<string>.SuccessResponse("Emergency contact deleted successfully.");
    }
}
