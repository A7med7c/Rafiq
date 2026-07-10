using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Prescriptions.DTOs;

namespace Rafiq.Application.Features.Prescriptions.Queries.GetMyPrescriptions;

/// <summary>
/// Returns all Prescriptions that belong to the currently authenticated user.
/// </summary>
public sealed record GetMyPrescriptionsQuery(Guid ProfileId)
    : IRequest<ApiResponse<List<PrescriptionResponseDto>>>;
