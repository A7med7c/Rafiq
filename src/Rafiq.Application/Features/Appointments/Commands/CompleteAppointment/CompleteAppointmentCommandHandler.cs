using MediatR;
using Microsoft.Extensions.Logging;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Appointments.DTOs;
using Rafiq.Domain.Exceptions;
using Rafiq.Domain.Repositories;

namespace Rafiq.Application.Features.Appointments.Commands.CompleteAppointment;

public sealed class CompleteAppointmentCommandHandler(
    IHealthProfileAuthorizationService authorizationService,
    IAppointmentRepository appointmentRepository,
    IWhatsAppService whatsAppService,
    IIdentityService identityService,
    ILogger<CompleteAppointmentCommandHandler> logger,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CompleteAppointmentCommand, ApiResponse<AppointmentResponseDto>>
{
    public async Task<ApiResponse<AppointmentResponseDto>> Handle(CompleteAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await appointmentRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        
        if (appointment is null) throw new NotFoundException(nameof(Domain.Entities.Documents.Appointment), request.Id);

        await authorizationService.EnsureCanWriteAsync(appointment.UserHealthProfileId, cancellationToken);

        appointment.MarkAsCompleted();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await SendWhatsAppAppointmentConfirmationAsync(appointment, cancellationToken);

        return ApiResponse<AppointmentResponseDto>.SuccessResponse(appointment.ToDto(), "Appointment completed successfully.");
    }

    private async Task SendWhatsAppAppointmentConfirmationAsync(Domain.Entities.Documents.Appointment appointment, CancellationToken cancellationToken)
    {
        var profile = appointment.UserHealthProfile;
        if (profile?.UserId is null)
        {
            logger.LogWarning("Profile {ProfileId} has no associated user. Skipping WhatsApp confirmation.", appointment.UserHealthProfileId);
            return;
        }

        var user = await identityService.GetByIdAsync(profile.UserId.Value, cancellationToken);
        if (string.IsNullOrEmpty(user?.PhoneNumber))
        {
            logger.LogWarning("No phone number found for user {UserId}. Skipping WhatsApp confirmation.", profile.UserId);
            return;
        }

        var patientName = $"{profile.FirstName} {profile.LastName}";
        var appointmentDate = appointment.AppointmentDateTime.ToString("dd/MM/yyyy");
        var appointmentTime = appointment.AppointmentDateTime.ToString("h:mm tt");
        var templateName = "appointments";

        logger.LogInformation(
            "Sending WhatsApp confirmation template '{Template}' to '{Phone}' for appointment {AppointmentId}.",
            templateName, user.PhoneNumber, appointment.Id);

        try
        {
            await whatsAppService.SendTemplateAsync(
                user.PhoneNumber,
                templateName,
                [patientName, appointment.Provider, appointmentDate, appointmentTime],
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send WhatsApp confirmation for appointment {AppointmentId}.", appointment.Id);
        }
    }
}
