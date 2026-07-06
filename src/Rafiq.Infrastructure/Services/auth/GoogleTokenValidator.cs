using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Rafiq.Application.Common.Interfaces;
using Rafiq.Application.Features.Auth.DTOs;

namespace Rafiq.Infrastructure.Services.auth
{
    public class GoogleTokenValidator(IConfiguration _configuration) : IGoogleTokenValidator
    {
        public async Task<GoogleUserInfoDto> ValidateAsync(string IdToken, CancellationToken cancellationToken)
        {
            var clientId = _configuration["Authentication:Google:ClientId"].Trim();

            Console.WriteLine($"[{clientId}]");
            Console.WriteLine(clientId.Length);

            var payload = await GoogleJsonWebSignature.ValidateAsync(IdToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[]
                    {
                        _configuration["Authentication:Google:ClientId"]?.Trim()
                    }
                });


            return new GoogleUserInfoDto
                (
                payload.Subject,
                payload.Email,
                payload.GivenName,
                payload.FamilyName,
                payload.Picture,
                payload.EmailVerified
            );
        }
    }
}
