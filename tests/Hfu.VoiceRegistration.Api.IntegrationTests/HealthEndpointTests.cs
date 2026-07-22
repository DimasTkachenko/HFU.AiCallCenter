using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Hfu.VoiceRegistration.Api.IntegrationTests;

public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealthReturnsHealthyStatus()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var health = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(health);
        Assert.Equal("healthy", health.Status);
        Assert.Equal("Hfu.VoiceRegistration.Api", health.Service);
        Assert.NotEqual(default, health.TimestampUtc);
    }

    private sealed record HealthResponse(
        string Status,
        string Service,
        DateTimeOffset TimestampUtc,
        string? Version);
}
