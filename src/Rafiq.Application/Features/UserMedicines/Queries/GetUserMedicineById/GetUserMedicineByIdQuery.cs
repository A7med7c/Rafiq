using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.UserMedicines.DTOs;

namespace Rafiq.Application.Features.UserMedicines.Queries.GetUserMedicineById;

/// <summary>
/// Returns a single UserMedicine by its Id.
/// Only succeeds if the medicine belongs to the currently authenticated user.
/// </summary>
public sealed record GetUserMedicineByIdQuery(Guid Id)
    : IRequest<ApiResponse<UserMedicineResponseDto>>;
