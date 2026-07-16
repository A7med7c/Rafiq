using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.PatientProfiles.Commands.Allergies.DeleteAllergy;

public sealed record DeleteAllergyCommand(
    Guid PatientProfileId,
    Guid AllergyId
) : IRequest<ApiResponse<object>>;
