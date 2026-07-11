using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.UserMedicines.DTOs;

namespace Rafiq.Application.Features.UserMedicines.Commands.AddFromPrescription;

public sealed record AddFromPrescriptionCommand(Guid ProfileId, List<Guid> PrescriptionMedicineIds)
    : IRequest<ApiResponse<AddFromPrescriptionResponseDto>>;
