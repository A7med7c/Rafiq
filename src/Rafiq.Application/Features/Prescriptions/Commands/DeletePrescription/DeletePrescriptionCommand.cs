using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.Prescriptions.Commands.DeletePrescription;

/// <summary>
/// Soft-deletes a Prescription by its ID.
/// Only succeeds if the prescription belongs to the currently authenticated user.
/// </summary>
public sealed record DeletePrescriptionCommand(Guid Id)
    : IRequest<ApiResponse<bool>>;
