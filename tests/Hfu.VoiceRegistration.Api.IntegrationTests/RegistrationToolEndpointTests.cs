using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Hfu.VoiceRegistration.Api.IntegrationTests;

public sealed class RegistrationToolEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RegistrationToolEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ToolEndpointsCanUpdateConfirmReadAndCompleteRegistration()
    {
        using var client = _factory.CreateClient();
        var sessionId = await ApiIntegrationTestHelpers.CreateSessionAsync(client);

        using var completeDocument = await ApiIntegrationTestHelpers.CompleteDemoRegistrationAsync(client, sessionId);
        var root = completeDocument.RootElement;

        Assert.True(root.GetProperty("succeeded").GetBoolean());
        Assert.Empty(root.GetProperty("errors").EnumerateArray());
        Assert.True(root.GetProperty("state").GetProperty("registrationCanBeCompleted").GetBoolean());
        Assert.Equal(
            "Харківська область",
            root.GetProperty("completion").GetProperty("finalRegistration").GetProperty("currentRegion").GetString());
        Assert.StartsWith(
            "DEMO-",
            root.GetProperty("completion").GetProperty("registrationResult").GetProperty("registrationId").GetString());

        using var stateResponse = await client.PostAsJsonAsync(
            $"/api/conversation-sessions/{sessionId}/tools/get-registration-state",
            new { });
        Assert.Equal(HttpStatusCode.OK, stateResponse.StatusCode);

        using var stateDocument = await ApiIntegrationTestHelpers.ReadJsonAsync(stateResponse);
        Assert.True(stateDocument.RootElement.GetProperty("succeeded").GetBoolean());
        Assert.Equal(sessionId, stateDocument.RootElement.GetProperty("state").GetProperty("sessionId").GetGuid());
    }

    [Fact]
    public async Task ToolBusinessErrorsReturnOkWithStructuredState()
    {
        using var client = _factory.CreateClient();
        var sessionId = await ApiIntegrationTestHelpers.CreateSessionAsync(client);

        using var response = await client.PostAsJsonAsync(
            $"/api/conversation-sessions/{sessionId}/tools/complete-registration",
            new
            {
                personalDataConsent = true,
                registrationConfirmed = true
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = await ApiIntegrationTestHelpers.ReadJsonAsync(response);
        var root = document.RootElement;

        Assert.False(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal(JsonValueKind.Object, root.GetProperty("state").ValueKind);
        Assert.Contains(
            root.GetProperty("errors").EnumerateArray(),
            error => error.GetProperty("code").GetString() == "RegistrationCannotBeCompleted");
    }

    [Fact]
    public async Task ToolEndpointForMissingSessionReturnsProblemDetails()
    {
        using var client = _factory.CreateClient();
        var missingSessionId = Guid.NewGuid();

        using var response = await client.PostAsJsonAsync(
            $"/api/conversation-sessions/{missingSessionId}/tools/update-registration-fields",
            new
            {
                fields = new[]
                {
                    new { name = "firstName", value = "Dimas" }
                }
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var document = await ApiIntegrationTestHelpers.ReadJsonAsync(response);
        Assert.Equal(404, document.RootElement.GetProperty("status").GetInt32());
    }
}
