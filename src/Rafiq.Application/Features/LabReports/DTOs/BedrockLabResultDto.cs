namespace Rafiq.Application.Features.LabReports.DTOs;

/// <summary>
/// Represents a single laboratory test as returned by Bedrock.
/// </summary>
public sealed class BedrockLabResultDto
{
    public string? TestName { get; set; }

    public string? Value { get; set; }

    public string? Unit { get; set; }

    public string? NormalRange { get; set; }

    public string? Status { get; set; }
}
