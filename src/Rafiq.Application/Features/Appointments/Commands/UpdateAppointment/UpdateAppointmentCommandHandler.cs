using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Appointments.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Appointments.Commands.UpdateAppointment;

public sealed class UpdateAppointmentCommandHandler(
    ICurrentUserService currentUserService,
    IAppointmentRepository appointmentRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateAppointmentCommand, ApiResponse<AppointmentResponseDto>>
{
    public async Task<ApiResponse<AppointmentResponseDto>> Handle(
        UpdateAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var appointment = await appointmentRepository
            .GetByIdAsync(request.Id, userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Documents.Appointment), request.Id);

        var duplicateExists = await appointmentRepository.ExistsDuplicateAsync(
            userId,
            request.AppointmentType,
            request.Title,
            request.Provider,
            request.AppointmentDateTime,
            request.Id,
            cancellationToken);

        if (duplicateExists)
            throw new ValidationException(new[] { "An appointment with the same type, title, provider, and date/time already exists." });

        appointment.UpdateDetails(
            request.AppointmentType,
            request.CustomType,
            request.Title,
            request.Provider,
            request.AppointmentDateTime,
            request.ReminderOffsetMinutes,
            request.Notes,
            request.Status);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<AppointmentResponseDto>.SuccessResponse(
            appointment.ToDto(),
            "Appointment updated successfully.");
    }
}
