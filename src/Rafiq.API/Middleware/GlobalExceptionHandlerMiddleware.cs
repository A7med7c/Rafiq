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
        if (context.Response.HasStarted)
        {
            _logger.LogError(exception,
                "Unhandled exception occurred after the response started. Method: {Method}, Path: {Path}, TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);
            throw exception;
        }

        var (statusCode, message, errors, errorCode) = exception switch
        {
            ValidationException validationException => (HttpStatusCode.BadRequest, "Validation failed.", validationException.Errors, (string?)null),
            DocumentValidationException documentValidationException => (HttpStatusCode.BadRequest, documentValidationException.Message, new[] { documentValidationException.Message }, documentValidationException.ErrorCode),
            BadRequestException badRequestException => (HttpStatusCode.BadRequest, badRequestException.Message, new[] { badRequestException.Message }, (string?)null),
            Rafiq.Domain.Exceptions.AuthenticationException authenticationException => (HttpStatusCode.Unauthorized, authenticationException.Message, new[] { authenticationException.Message }, (string?)null),
            UnauthorizedException unauthorizedException => (HttpStatusCode.Unauthorized, unauthorizedException.Message, new[] { unauthorizedException.Message }, (string?)null),
            NotFoundException notFoundException => (HttpStatusCode.NotFound, notFoundException.Message, new[] { notFoundException.Message }, (string?)null),
            ConflictException conflictException => (HttpStatusCode.Conflict, conflictException.Message, new[] { conflictException.Message }, (string?)null),
            ExternalServiceException externalServiceException => (HttpStatusCode.BadGateway, "External service error.", new[] { externalServiceException.Message }, (string?)null),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", new[] { "An unexpected error occurred." }, (string?)null)
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception,
                "Unhandled exception occurred. Method: {Method}, Path: {Path}, TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);
        }
        else
        {
            _logger.LogWarning(exception,
                "Handled exception occurred: {ExceptionType}. Method: {Method}, Path: {Path}, TraceId: {TraceId}",
                exception.GetType().Name,
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse<object>.FailureResponse(message, errors.ToArray(), errorCode);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
