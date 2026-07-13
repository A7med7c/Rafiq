using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.EmergencyContacts.Commands.DeleteEmergencyContact;

public sealed record DeleteEmergencyContactCommand(Guid Id)
    : IRequest<ApiResponse<string>>;
