using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Rafiq.API;

namespace Rafiq.API.Tests;

public sealed class AuthControllerTests
{
    [Fact]
    public async Task ProtectedAuthEndpoint_WithoutToken_ReturnsApiResponseEnvelope()
    {
        Environment.SetEnvironmentVariable("JWT_SECRET_KEY", "test-secret-key-with-enough-length-for-hmac-sha256");

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:SecretKey"] = "test-secret-key-with-enough-length-for-hmac-sha256",
                        ["Jwt:Issuer"] = "Rafiq",
                        ["Jwt:Audience"] = "Rafiq"
                    });
                });
            });

        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/auth/revoke-token", JsonContent.Create(new { refreshToken = "token" }));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        document.RootElement.GetProperty("success").GetBoolean().Should().BeFalse();
        document.RootElement.GetProperty("message").GetString().Should().Be("Authentication required.");
    }
}
