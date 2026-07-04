namespace Rafiq.Application.Features.LabReports.DTOs;

/// <summary>
/// The client-facing response for a single laboratory test result.
/// </summary>
public sealed class LabResultResponseDto
{
    public Guid Id { get; init; }

    public string TestName { get; init; } = null!;

    public string? Value { get; init; }

    public string? Unit { get; init; }

    public string? NormalRange { get; init; }

    public string? Status { get; init; }
}
