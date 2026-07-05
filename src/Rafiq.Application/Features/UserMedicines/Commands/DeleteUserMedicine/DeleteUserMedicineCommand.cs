using MediatR;
using Rafiq.Application.Common.Models;

namespace Rafiq.Application.Features.UserMedicines.Commands.DeleteUserMedicine;

/// <summary>
/// Soft-deletes a UserMedicine by its ID.
/// Only succeeds if the medicine belongs to the currently authenticated user.
/// </summary>
public sealed record DeleteUserMedicineCommand(Guid Id)
    : IRequest<ApiResponse<bool>>;
