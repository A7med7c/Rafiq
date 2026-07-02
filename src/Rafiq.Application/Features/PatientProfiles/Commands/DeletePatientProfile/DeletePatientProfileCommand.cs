using MediatR;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Domain.Enums;

namespace Rafiq.Application.Features.PatientProfiles.Commands.DeletePatientProfile;

public sealed record DeletePatientProfileCommand(Guid PatientProfileId)
    : IRequest<ApiResponse<object>>, IPatientOwnedRequest
{
    public string EntityType => "PatientProfile";
    public Guid? EntityId => PatientProfileId;
}
