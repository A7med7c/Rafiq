using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.EmergencyContacts.DTOs;

namespace Rafiq.Application.Features.EmergencyContacts.Commands.CreateEmergencyContact;

public sealed record CreateEmergencyContactCommand(
    string Name,
    string PhoneNumber,
    string Relation)
    : IRequest<ApiResponse<EmergencyContactResponseDto>>;
