namespace Rafiq.Application.Common.Models;

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<string>? Errors { get; init; }

    public static ApiResponse<T> SuccessResponse(T? data, string message = "OK")
        => new() { Success = true, Data = data, Message = message, Errors = null };

    public static ApiResponse<T> FailureResponse(string message, IReadOnlyList<string>? errors = null)
        => new() { Success = false, Data = default, Message = message, Errors = errors ?? Array.Empty<string>() };
}
