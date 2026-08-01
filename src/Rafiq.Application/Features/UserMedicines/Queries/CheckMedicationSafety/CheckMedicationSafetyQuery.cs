using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.UserMedicines.Queries.CheckMedicationSafety;

public sealed record AllergyCheckResultDto(
    bool IsSafe,
    string RiskLevel,
    string? TriggeredAllergy,
    string? Explanation);

public sealed record CheckMedicationSafetyQuery(Guid ProfileId, string MedicationName)
    : IRequest<ApiResponse<AllergyCheckResultDto>>;
