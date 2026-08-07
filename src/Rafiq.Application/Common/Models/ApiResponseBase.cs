namespace Rafiq.Application.Common.Models;

public class ApiResponseBase
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public IReadOnlyList<string>? Errors { get; init; }

    public string? ErrorCode { get; init; }

    public IReadOnlyList<string>? Warnings { get; init; }

    public object? ErrorData { get; init; }

    public static ApiResponseBase SuccessResponse(
    string message = "OK",
    IReadOnlyList<string>? warnings = null)
    => new()
    {
        Success = true,
        Message = message,
        Warnings = warnings
    };

    public static ApiResponseBase FailureResponse(
        string message,
        IReadOnlyList<string>? errors = null,
        string? errorCode = null,
        object? errorData = null)
        => new()
        {
            Success = false,
            Message = message,
            Errors = errors ?? Array.Empty<string>(),
            ErrorCode = errorCode,
            ErrorData = errorData
        };
}