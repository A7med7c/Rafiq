using MediatR;
using Microsoft.AspNetCore.Http;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Common.Models;
using Rafiq.Application.Features.Prescriptions.DTOs;

namespace Rafiq.Application.Features.Prescriptions.Commands.UploadPrescription;

/// <summary>
/// Uploads a prescription image, analyzes it with Bedrock,
/// saves the extracted data, and returns the result.
/// UserId is NEVER accepted from the client — it comes from the JWT.
/// </summary>
public sealed record UploadPrescriptionCommand(Guid ProfileId, IFormFile Image)
    : IRequest<ApiResponse<PrescriptionResponseDto>>, IAiCommand;
