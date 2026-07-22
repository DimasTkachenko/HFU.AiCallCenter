using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Hfu.VoiceRegistration.Api.IntegrationTests;

public sealed class ConversationSessionEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ConversationSessionEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostConversationSessionsCreatesSessionWithInitialState()
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/conversation-sessions", new { });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        using var document = await ApiIntegrationTestHelpers.ReadJsonAsync(response);
        var root = document.RootElement;
        var sessionId = root.GetProperty("sessionId").GetGuid();

        Assert.NotEqual(Guid.Empty, sessionId);
        Assert.Equal("Created", root.GetProperty("status").GetString());
        Assert.Equal(0, root.GetProperty("version").GetInt64());
        Assert.False(root.GetProperty("state").GetProperty("registrationCanBeCompleted").GetBoolean());
        Assert.Equal(sessionId, root.GetProperty("state").GetProperty("sessionId").GetGuid());
    }

    [Fact]
    public async Task GetConversationSessionReturnsSessionState()
    {
        using var client = _factory.CreateClient();
        var sessionId = await ApiIntegrationTestHelpers.CreateSessionAsync(client);

        using var response = await client.GetAsync($"/api/conversation-sessions/{sessionId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = await ApiIntegrationTestHelpers.ReadJsonAsync(response);
        var root = document.RootElement;

        Assert.Equal(sessionId, root.GetProperty("sessionId").GetGuid());
        Assert.Equal("Created", root.GetProperty("status").GetString());
        Assert.True(root.GetProperty("events").GetArrayLength() >= 0);
        Assert.False(root.GetProperty("state").GetProperty("registrationCanBeCompleted").GetBoolean());
    }

    [Fact]
    public async Task GetMissingConversationSessionReturnsProblemDetails()
    {
        using var client = _factory.CreateClient();
        var missingSessionId = Guid.NewGuid();

        using var response = await client.GetAsync($"/api/conversation-sessions/{missingSessionId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var document = await ApiIntegrationTestHelpers.ReadJsonAsync(response);
        Assert.Equal(404, document.RootElement.GetProperty("status").GetInt32());
        Assert.Contains(
            missingSessionId.ToString(),
            document.RootElement.GetProperty("detail").GetString(),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostAbandonMarksActiveSessionAbandoned()
    {
        using var client = _factory.CreateClient();
        var sessionId = await ApiIntegrationTestHelpers.CreateSessionAsync(client);

        using var response = await client.PostAsJsonAsync(
            $"/api/conversation-sessions/{sessionId}/abandon",
            new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = await ApiIntegrationTestHelpers.ReadJsonAsync(response);
        Assert.Equal(sessionId, document.RootElement.GetProperty("sessionId").GetGuid());
        Assert.Equal("Abandoned", document.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task PostAbandonCompletedSessionReturnsConflictProblemDetails()
    {
        using var client = _factory.CreateClient();
        var sessionId = await ApiIntegrationTestHelpers.CreateSessionAsync(client);
        using var completed = await ApiIntegrationTestHelpers.CompleteDemoRegistrationAsync(client, sessionId);
        Assert.True(completed.RootElement.GetProperty("succeeded").GetBoolean());

        using var response = await client.PostAsJsonAsync(
            $"/api/conversation-sessions/{sessionId}/abandon",
            new { });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var document = await ApiIntegrationTestHelpers.ReadJsonAsync(response);
        Assert.Equal(409, document.RootElement.GetProperty("status").GetInt32());
    }
}
