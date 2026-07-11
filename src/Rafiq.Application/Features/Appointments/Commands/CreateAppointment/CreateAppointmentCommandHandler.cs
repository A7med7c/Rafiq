using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Appointments.DTOs;
using Rafiq.Domain.Entities.Documents;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Appointments.Commands.CreateAppointment;

public sealed class CreateAppointmentCommandHandler(
    IPatientProfileRepository patientProfileRepository,
    IHealthProfileAuthorizationService authorizationService,
    IAppointmentRepository appointmentRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateAppointmentCommand, ApiResponse<AppointmentResponseDto>>
{
    public async Task<ApiResponse<AppointmentResponseDto>> Handle(
        CreateAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        _ = await patientProfileRepository.GetByIdAsync(request.ProfileId, cancellationToken)
            ?? throw new NotFoundException("UserHealthProfile", request.ProfileId);

        await authorizationService.EnsureCanWriteAsync(request.ProfileId, cancellationToken);

        var duplicateExists = await appointmentRepository.ExistsDuplicateAsync(
            request.ProfileId,
            request.AppointmentType,
            request.Title,
            request.Provider,
            request.AppointmentDateTime,
            null,
            cancellationToken);

        if (duplicateExists)
            throw new ValidationException(new[] { "An appointment with the same type, title, provider, and date/time already exists." });

        var appointment = new Appointment(
            request.ProfileId,
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
