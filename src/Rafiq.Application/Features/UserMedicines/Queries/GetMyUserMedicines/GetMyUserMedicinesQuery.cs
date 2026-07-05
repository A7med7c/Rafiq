using MediatR;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.UserMedicines.DTOs;

namespace Rafiq.Application.Features.UserMedicines.Queries.GetMyUserMedicines;

/// <summary>
/// Returns all UserMedicines that belong to the currently authenticated user.
/// </summary>
public sealed record GetMyUserMedicinesQuery
    : IRequest<ApiResponse<List<UserMedicineResponseDto>>>;
