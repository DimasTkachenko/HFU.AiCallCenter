using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Hfu.VoiceRegistration.Api.IntegrationTests;

public sealed class SwaggerEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SwaggerEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSwaggerJsonIncludesStage7Endpoints()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var paths = document.RootElement.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/conversation-sessions", out _));
        Assert.True(paths.TryGetProperty("/api/reference-data/regions", out _));
        Assert.True(paths.TryGetProperty("/api/conversation-sessions/{sessionId}/tools/complete-registration", out _));
    }
}
