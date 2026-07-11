using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Rafiq.Application.Common.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using System.Security.Claims;
using System.Text;

namespace Rafiq.Infrastructure.Services.Auth;

public sealed class ResetTokenService(IConfiguration configuration)
    : IResetTokenService
{
    public string GenerateResetToken(Guid userId)
    {
        var secret =
            Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
            ?? configuration["Jwt:SecretKey"];

        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("JWT Secret Key is missing.");

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secret));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),

            new Claim("purpose", "password-reset")
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public Guid ValidateResetToken(string token)
    {
        try
        {
            var secret =
                Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
                ?? configuration["Jwt:SecretKey"];

            var handler = new JwtSecurityTokenHandler();

            var principal = handler.ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],

                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(secret!))
                },
                out _);

            var purpose = principal.FindFirstValue("purpose");

            if (purpose != "password-reset")
                throw new AuthenticationException("Invalid reset session.");

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                throw new AuthenticationException("Invalid reset session.");

            return Guid.Parse(userId);
        }
        catch (SecurityTokenExpiredException)
        {
            throw new AuthenticationException(
                "Reset session has expired. Please verify the OTP again.");
        }
        catch (SecurityTokenException)
        {
            throw new AuthenticationException(
                "Invalid reset session.");
        }
    }
}