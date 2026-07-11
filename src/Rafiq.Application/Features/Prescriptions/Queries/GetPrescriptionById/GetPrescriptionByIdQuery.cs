using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Prescriptions.DTOs;

namespace Rafiq.Application.Features.Prescriptions.Queries.GetPrescriptionById;

/// <summary>
/// Returns a single Prescription by its Id.
/// Only succeeds if the prescription belongs to the currently authenticated user.
/// </summary>
public sealed record GetPrescriptionByIdQuery(Guid Id)
    : IRequest<ApiResponse<PrescriptionResponseDto>>;
