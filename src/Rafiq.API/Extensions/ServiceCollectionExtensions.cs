using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Rafiq.Application.Common.Models;
using System.Security.Claims;
using System.Text;

namespace Rafiq.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Rafiq API",
                Version = "v1",
                Description = "Interactive API documentation for testing Rafiq authentication and patient profile endpoints."
            });

            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter a valid JWT access token. The Bearer prefix is added automatically."
            });

            options.OperationFilter<SwaggerAuthorizeOperationFilter>();
            options.DocumentFilter<SwaggerAuthorizeDocumentFilter>();
        });

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? hostEnvironment = null)
    {
        var isDevelopment = hostEnvironment?.IsDevelopment() ?? false;
        var secret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? configuration["Jwt:SecretKey"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("JWT secret key must be provided through JWT_SECRET_KEY or Jwt:SecretKey configuration.");
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
         // .AddCookie().AddGoogle(options =>
         // {
         //     var clientId = configuration["Authentication:Google:ClientId"];
         //     if (clientId is null)
         //         throw new ArgumentNullException(nameof(clientId));


         //     var clientSecret = configuration["Authentication:Google:ClientSecret"];
         //     if (clientSecret is null)
         //         throw new ArgumentNullException(nameof(clientId));
         //     options.ClientId = clientId;
         //     options.ClientSecret = clientSecret;
         //     options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;})
         .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("Rafiq.Auth.Diagnostics");

                        bool hasHeader = context.Request.Headers.TryGetValue("Authorization", out var headerValues);
                        var headerValue = headerValues.ToString();
                        
                        logger.LogInformation(
                            "[JWT: OnMessageReceived] Method: {Method}, Path: {Path}, Scheme: {Scheme}, AuthorizationHeaderExists: {Exists}, HeaderLength: {Length}",
                            context.Request.Method,
                            context.Request.Path,
                            context.Scheme.Name,
                            hasHeader,
                            headerValue?.Length ?? 0);

                        if (hasHeader)
                        {
                            var headers = context.Request.Headers["Authorization"];
                            logger.LogWarning("RAW header count={Count}, values={Values}", 
                                headers.Count, string.Join(" ||| ", headers.ToArray()));
                        }

                        if (hasHeader && headerValue != null && headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            var token = headerValue.Substring("Bearer ".Length).Trim();
                            logger.LogInformation("[JWT: OnMessageReceived] Extracted Token Length: {TokenLength}", token.Length);
                            if (token.Length > 20)
                            {
                                logger.LogInformation("[JWT: OnMessageReceived] Extracted Token Prefix/Suffix: {Prefix}...{Suffix}",
                                    token.Substring(0, 10), token.Substring(token.Length - 10));
                            }
                        }

                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/notifications"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("Rafiq.Auth.Diagnostics");

                        var jwtToken = context.SecurityToken as Microsoft.IdentityModel.JsonWebTokens.JsonWebToken;
                        var tokenIssuer = jwtToken?.Issuer ?? context.SecurityToken?.Issuer ?? "none";
                        var tokenAudience = "none";
                        
                        if (jwtToken != null && jwtToken.TryGetPayloadValue("aud", out object audObj))
                        {
                            tokenAudience = audObj?.ToString() ?? "none";
                        }

                        logger.LogWarning(
                            "[JWT: OnTokenValidated] Token validation SUCCESS for Method: {Method}, Path: {Path}. Validated Issuer: {Issuer}, Validated Audience: {Audience}",
                            context.Request.Method, context.Request.Path, tokenIssuer, tokenAudience);

                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("Rafiq.Auth.Diagnostics");

                        logger.LogError(
                            "[JWT: OnAuthenticationFailed] FAILED on Method: {Method}, Path: {Path}. Exception Type: {ExceptionType}. Message: {Message}. InnerException: {InnerException}",
                            context.Request.Method,
                            context.Request.Path,
                            context.Exception.GetType().Name,
                            context.Exception.Message,
                            context.Exception.InnerException?.Message ?? "None");

                        return Task.CompletedTask;
                    },
                    OnChallenge = async context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("Rafiq.Auth.Diagnostics");

                        logger.LogWarning(
                            "[JWT: OnChallenge] Challenge on Method: {Method}, Path: {Path}. Error: {Error}. ErrorDescription: {ErrorDescription}. AuthenticateFailure: {Failure}",
                            context.Request.Method,
                            context.Request.Path,
                            context.Error ?? "None",
                            context.ErrorDescription ?? "None",
                            context.AuthenticateFailure?.Message ?? "None");

                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        var response = ApiResponse<object>.FailureResponse(
                            "Authentication required.",
                            new[] { "Access token is missing, invalid, or expired." });
                        await context.Response.WriteAsJsonAsync(response);
                    },
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";
                        var response = ApiResponse<object>.FailureResponse(
                            "Access denied.",
                            new[] { "You do not have permission to access this resource." });
                        await context.Response.WriteAsJsonAsync(response);
                    }
                };
            });

        return services;
    }
}
