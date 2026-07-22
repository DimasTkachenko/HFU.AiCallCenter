using System.Net;
using System.Text;
using Hfu.VoiceRegistration.Api.OpenAIRealtime;
using Microsoft.Extensions.Options;

namespace Hfu.VoiceRegistration.Api.IntegrationTests;

public sealed class OpenAIRealtimeClientTests
{
    [Fact]
    public async Task CreateCallAsyncSendsSdpAndSessionConfigToOpenAI()
    {
        var handler = new CapturingHttpMessageHandler();
        using var httpClient = new HttpClient(handler);
        var client = new OpenAIRealtimeClient(
            httpClient,
            Options.Create(new OpenAIRealtimeOptions
            {
                ApiKey = "sk-test",
                BaseUrl = "https://api.openai.test/v1"
            }));

        var result = await client.CreateCallAsync(
            "offer-sdp",
            "hfu-session-test",
            CancellationToken.None);

        Assert.Equal("answer-sdp", result.SdpAnswer);
        Assert.NotNull(handler.Request);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal(
            new Uri("https://api.openai.test/v1/realtime/calls"),
            handler.Request.RequestUri);
        Assert.Equal("Bearer", handler.Request.Headers.Authorization?.Scheme);
        Assert.Equal("sk-test", handler.Request.Headers.Authorization?.Parameter);
        Assert.True(handler.Request.Headers.TryGetValues(
            "OpenAI-Safety-Identifier",
            out var safetyIdentifiers));
        Assert.Contains("hfu-session-test", safetyIdentifiers);
        Assert.StartsWith("multipart/form-data", handler.ContentType);
        Assert.Contains("name=sdp", handler.Body);
        Assert.Contains("offer-sdp", handler.Body);
        Assert.Contains("name=session", handler.Body);
        Assert.Contains("\"model\":\"gpt-realtime-2.1\"", handler.Body);
        Assert.Contains("\"voice\":\"marin\"", handler.Body);
        Assert.Contains("gpt-realtime-whisper", handler.Body);
        Assert.Contains("\"tool_choice\":\"auto\"", handler.Body);
        Assert.Contains("\"name\":\"update_registration_fields\"", handler.Body);
        Assert.Contains("\"name\":\"confirm_registration_fields\"", handler.Body);
        Assert.Contains("\"name\":\"mark_fields_for_clarification\"", handler.Body);
        Assert.Contains("\"name\":\"clear_registration_fields\"", handler.Body);
        Assert.Contains("\"name\":\"get_registration_state\"", handler.Body);
        Assert.Contains("\"name\":\"complete_registration\"", handler.Body);
        Assert.Contains("\"personalDataConsent\"", handler.Body);
        Assert.Contains("\"registrationConfirmed\"", handler.Body);
        Assert.DoesNotContain("sk-test", handler.Body);
    }

    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        public string ContentType { get; private set; } = "";

        public string Body { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            ContentType = request.Content?.Headers.ContentType?.ToString() ?? "";
            Body = request.Content is null
                ? ""
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("answer-sdp", Encoding.UTF8, "application/sdp")
            };
        }
    }
}
