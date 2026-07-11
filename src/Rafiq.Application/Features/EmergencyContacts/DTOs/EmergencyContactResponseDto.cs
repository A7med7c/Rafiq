namespace Rafiq.Application.Features.EmergencyContacts.DTOs;

public sealed record EmergencyContactResponseDto(
    Guid Id,
    Guid UserId,
    string Name,
    string PhoneNumber,
    string Relation);
