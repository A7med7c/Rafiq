using Rafiq.Application.Common.Models;
using Rafiq.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace Rafiq.API.Middleware;

public sealed class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, errors) = exception switch
        {
            ValidationException validationException => (HttpStatusCode.BadRequest, "Validation failed.", validationException.Errors),
            Rafiq.Domain.Exceptions.AuthenticationException authenticationException => (HttpStatusCode.Unauthorized, authenticationException.Message, new[] { authenticationException.Message }),
            UnauthorizedException unauthorizedException => (HttpStatusCode.Forbidden, unauthorizedException.Message, new[] { unauthorizedException.Message }),
            NotFoundException notFoundException => (HttpStatusCode.NotFound, notFoundException.Message, new[] { notFoundException.Message }),
            ConflictException conflictException => (HttpStatusCode.Conflict, conflictException.Message, new[] { conflictException.Message }),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", new[] { "An unexpected error occurred." })
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred.");
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception occurred: {ExceptionType}", exception.GetType().Name);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse<object>.FailureResponse(message, errors.ToArray());
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
