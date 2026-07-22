using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;

namespace Hfu.VoiceRegistration.Api.IntegrationTests;

public sealed class ConversationRealtimeHubTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ConversationRealtimeHubTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ConnectsToConversationHub()
    {
        await using var connection = CreateConnection();

        await connection.StartAsync();

        Assert.Equal(HubConnectionState.Connected, connection.State);
    }

    [Fact]
    public async Task JoinedSessionReceivesRegistrationStateChangedEventAfterUpdate()
    {
        using var client = _factory.CreateClient();
        await using var connection = CreateConnection();
        var received = new TaskCompletionSource<JsonElement>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        connection.On<JsonElement>(
            "ConversationEvent",
            conversationEvent => received.TrySetResult(conversationEvent));

        await connection.StartAsync();
        var sessionId = await ApiIntegrationTestHelpers.CreateSessionAsync(client);
        await connection.InvokeAsync("JoinSession", sessionId);

        using var response = await client.PostAsJsonAsync(
            $"/api/conversation-sessions/{sessionId}/tools/update-registration-fields",
            new
            {
                fields = new[]
                {
                    new { name = "firstName", value = "Dimas" }
                }
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var conversationEvent = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(sessionId, conversationEvent.GetProperty("sessionId").GetGuid());
        Assert.Equal("RegistrationStateChanged", conversationEvent.GetProperty("type").GetString());
        Assert.True(conversationEvent.GetProperty("version").GetInt64() > 0);
        Assert.Equal(
            "Registration state changed.",
            conversationEvent.GetProperty("message").GetString());
    }

    private HubConnection CreateConnection()
    {
        return new HubConnectionBuilder()
            .WithUrl(
                "http://localhost/hubs/conversation",
                options =>
                {
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                    options.Transports = HttpTransportType.LongPolling;
                })
            .Build();
    }
}
