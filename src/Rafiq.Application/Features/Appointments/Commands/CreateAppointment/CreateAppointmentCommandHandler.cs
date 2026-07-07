using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Appointments.DTOs;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Enums;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Appointments.Commands.CreateAppointment;

public sealed class CreateAppointmentCommandHandler(
    ICurrentUserService currentUserService,
    IAppointmentRepository appointmentRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateAppointmentCommand, ApiResponse<AppointmentResponseDto>>
{
    public async Task<ApiResponse<AppointmentResponseDto>> Handle(
        CreateAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new UnauthorizedException("Authentication is required.");

        var duplicateExists = await appointmentRepository.ExistsDuplicateAsync(
            userId,
            request.AppointmentType,
            request.Title,
            request.Provider,
            request.AppointmentDateTime,
            null,
            cancellationToken);

        if (duplicateExists)
            throw new ValidationException(new[] { "An appointment with the same type, title, provider, and date/time already exists." });

        var appointment = new Appointment(
            userId,
            request.AppointmentType,
            request.CustomType,
            request.Title,
            request.Provider,
            request.AppointmentDateTime,
            request.ReminderOffsetMinutes,
            request.Notes);

        await appointmentRepository.AddAsync(appointment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<AppointmentResponseDto>.SuccessResponse(
            appointment.ToDto(),
            "Appointment created successfully.");
    }
}
