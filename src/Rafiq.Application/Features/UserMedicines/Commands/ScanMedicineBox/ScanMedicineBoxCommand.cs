using MediatR;
using Microsoft.AspNetCore.Http;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.UserMedicines.DTOs;

namespace Rafiq.Application.Features.UserMedicines.Commands.ScanMedicineBox;

/// <summary>
/// Scans a medicine box image, analyzes it with Bedrock,
/// and returns the extracted information along with the uploaded image path.
/// Does NOT save to the database directly.
/// </summary>
public sealed record ScanMedicineBoxCommand(IFormFile Image)
    : IRequest<ApiResponse<ScanMedicineBoxResponseDto>>;
