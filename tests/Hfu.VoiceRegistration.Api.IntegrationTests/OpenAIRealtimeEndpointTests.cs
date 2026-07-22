using System.Net;
using System.Text;
using Hfu.VoiceRegistration.Api.OpenAIRealtime;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hfu.VoiceRegistration.Api.IntegrationTests;

public sealed class OpenAIRealtimeEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OpenAIRealtimeEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostRealtimeCallReturnsSdpAnswer()
    {
        var fakeClient = new CapturingOpenAIRealtimeClient("answer-sdp");
        using var client = CreateClientWithRealtimeClient(fakeClient);
        var sessionId = await ApiIntegrationTestHelpers.CreateSessionAsync(client);

        using var response = await client.PostAsync(
            $"/api/conversation-sessions/{sessionId}/realtime/calls",
            SdpContent("offer-sdp"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/sdp", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("answer-sdp", await response.Content.ReadAsStringAsync());
        Assert.Equal("offer-sdp", fakeClient.SdpOffer);
        Assert.Equal($"hfu-session-{sessionId:N}", fakeClient.SafetyIdentifier);
    }

    [Fact]
    public async Task PostRealtimeCallForMissingSessionReturnsNotFound()
    {
        using var client = CreateClientWithRealtimeClient(new CapturingOpenAIRealtimeClient());
        var missingSessionId = Guid.NewGuid();

        using var response = await client.PostAsync(
            $"/api/conversation-sessions/{missingSessionId}/realtime/calls",
            SdpContent("offer-sdp"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PostRealtimeCallWithBlankSdpReturnsBadRequest()
    {
        var fakeClient = new CapturingOpenAIRealtimeClient();
        using var client = CreateClientWithRealtimeClient(fakeClient);
        var sessionId = await ApiIntegrationTestHelpers.CreateSessionAsync(client);

        using var response = await client.PostAsync(
            $"/api/conversation-sessions/{sessionId}/realtime/calls",
            SdpContent("  "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Null(fakeClient.SdpOffer);
    }

    [Fact]
    public async Task PostRealtimeCallWithNonSdpContentTypeReturnsUnsupportedMediaType()
    {
        var fakeClient = new CapturingOpenAIRealtimeClient();
        using var client = CreateClientWithRealtimeClient(fakeClient);
        var sessionId = await ApiIntegrationTestHelpers.CreateSessionAsync(client);

        using var response = await client.PostAsync(
            $"/api/conversation-sessions/{sessionId}/realtime/calls",
            new StringContent("offer-sdp", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Null(fakeClient.SdpOffer);
    }

    [Fact]
    public async Task PostRealtimeCallWithOversizedSdpReturnsPayloadTooLarge()
    {
        var fakeClient = new CapturingOpenAIRealtimeClient();
        using var client = CreateClientWithRealtimeClient(
            fakeClient,
            new Dictionary<string, string?>
            {
                ["OpenAI:RealtimeMaxSdpOfferCharacters"] = "8"
            });
        var sessionId = await ApiIntegrationTestHelpers.CreateSessionAsync(client);

        using var response = await client.PostAsync(
            $"/api/conversation-sessions/{sessionId}/realtime/calls",
            SdpContent("offer-sdp"));

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Null(fakeClient.SdpOffer);
    }

    [Fact]
    public async Task PostRealtimeCallForCompletedSessionReturnsConflict()
    {
        var fakeClient = new CapturingOpenAIRealtimeClient();
        using var client = CreateClientWithRealtimeClient(fakeClient);
        var sessionId = await ApiIntegrationTestHelpers.CreateSessionAsync(client);
        using var completed = await ApiIntegrationTestHelpers.CompleteDemoRegistrationAsync(client, sessionId);
        Assert.True(completed.RootElement.GetProperty("succeeded").GetBoolean());

        using var response = await client.PostAsync(
            $"/api/conversation-sessions/{sessionId}/realtime/calls",
            SdpContent("offer-sdp"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Null(fakeClient.SdpOffer);
    }

    [Fact]
    public async Task PostRealtimeCallWithoutApiKeyReturnsConfigurationFailure()
    {
        using var client = _factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["OpenAI:ApiKey"] = ""
                    });
                });
            })
            .CreateClient();
        var sessionId = await ApiIntegrationTestHelpers.CreateSessionAsync(client);

        using var response = await client.PostAsync(
            $"/api/conversation-sessions/{sessionId}/realtime/calls",
            SdpContent("offer-sdp"));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var document = await ApiIntegrationTestHelpers.ReadJsonAsync(response);
        Assert.Equal("Realtime configuration failure", document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task PostRealtimeCallTransportFailureReturnsBadGateway()
    {
        using var client = CreateClientWithRealtimeClient(
            new CapturingOpenAIRealtimeClient(
                exception: new HttpRequestException("Connection refused.")));
        var sessionId = await ApiIntegrationTestHelpers.CreateSessionAsync(client);

        using var response = await client.PostAsync(
            $"/api/conversation-sessions/{sessionId}/realtime/calls",
            SdpContent("offer-sdp"));

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PostRealtimeCallIsRateLimitedPerSession()
    {
        using var client = CreateClientWithRealtimeClient(
            new CapturingOpenAIRealtimeClient(),
            new Dictionary<string, string?>
            {
                ["OpenAI:RealtimeCallsPerMinute"] = "2"
            });
        var sessionId = await ApiIntegrationTestHelpers.CreateSessionAsync(client);

        using var first = await client.PostAsync(
            $"/api/conversation-sessions/{sessionId}/realtime/calls",
            SdpContent("offer-sdp-1"));
        using var second = await client.PostAsync(
            $"/api/conversation-sessions/{sessionId}/realtime/calls",
            SdpContent("offer-sdp-2"));
        using var third = await client.PostAsync(
            $"/api/conversation-sessions/{sessionId}/realtime/calls",
            SdpContent("offer-sdp-3"));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal((HttpStatusCode)429, third.StatusCode);
    }

    private HttpClient CreateClientWithRealtimeClient(
        IOpenAIRealtimeClient realtimeClient,
        IReadOnlyDictionary<string, string?>? configurationValues = null)
    {
        return _factory
            .WithWebHostBuilder(builder =>
            {
                if (configurationValues is not null)
                {
                    builder.ConfigureAppConfiguration((_, configuration) =>
                    {
                        configuration.AddInMemoryCollection(configurationValues);
                    });
                }

                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IOpenAIRealtimeClient>();
                    services.AddSingleton(realtimeClient);
                });
            })
            .CreateClient();
    }

    private static StringContent SdpContent(string sdp)
    {
        return new StringContent(sdp, Encoding.UTF8, "application/sdp");
    }

    private sealed class CapturingOpenAIRealtimeClient : IOpenAIRealtimeClient
    {
        private readonly string _sdpAnswer;
        private readonly Exception? _exception;

        public CapturingOpenAIRealtimeClient(
            string sdpAnswer = "answer-sdp",
            Exception? exception = null)
        {
            _sdpAnswer = sdpAnswer;
            _exception = exception;
        }

        public string? SdpOffer { get; private set; }

        public string? SafetyIdentifier { get; private set; }

        public Task<OpenAIRealtimeCallResult> CreateCallAsync(
            string sdpOffer,
            string safetyIdentifier,
            CancellationToken cancellationToken)
        {
            if (_exception is not null)
            {
                throw _exception;
            }

            SdpOffer = sdpOffer;
            SafetyIdentifier = safetyIdentifier;

            return Task.FromResult(new OpenAIRealtimeCallResult(_sdpAnswer));
        }
    }
}
