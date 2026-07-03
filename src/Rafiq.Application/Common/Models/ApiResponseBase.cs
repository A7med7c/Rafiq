namespace Rafiq.Application.Common.Models;

public class ApiResponseBase
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public IReadOnlyList<string>? Errors { get; init; }

    public static ApiResponseBase SuccessResponse(string message = "OK")
        => new()
        {
            Success = true,
            Message = message
        };

    public static ApiResponseBase FailureResponse(
        string message,
        IReadOnlyList<string>? errors = null)
        => new()
        {
            Success = false,
            Message = message,
            Errors = errors ?? Array.Empty<string>()
        };
}