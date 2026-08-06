namespace Rafiq.Application.Common.Models;

public sealed class ApiResponse<T> : ApiResponseBase
{
    public T? Data { get; init; }

    public new static ApiResponse<T> SuccessResponse(
        T? data,
        string message = "OK")
        => new()
        {
            Success = true,
            Data = data,
            Message = message
        };

    public new static ApiResponse<T> FailureResponse(
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