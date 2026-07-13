using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.EmergencyContacts.DTOs;

namespace Rafiq.Application.Features.EmergencyContacts.Queries.GetEmergencyContacts;

public sealed record GetEmergencyContactsQuery()
    : IRequest<ApiResponse<IReadOnlyList<EmergencyContactResponseDto>>>;
