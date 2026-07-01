using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Rafiq.API;

namespace Rafiq.API.Tests;

public sealed class PatientProfilesControllerTests
{
    [Fact]
    public async Task GetMe_WithoutToken_ReturnsApiResponseEnvelope()
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

        var response = await client.GetAsync("/api/patient-profiles/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        document.RootElement.GetProperty("success").GetBoolean().Should().BeFalse();
        document.RootElement.GetProperty("errors").EnumerateArray().Should().Contain(x => x.GetString() == "Access token is missing, invalid, or expired.");
    }
}
