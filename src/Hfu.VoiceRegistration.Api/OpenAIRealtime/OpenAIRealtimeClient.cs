using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Hfu.VoiceRegistration.Api.OpenAIRealtime;

public sealed class OpenAIRealtimeClient : IOpenAIRealtimeClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly IOptions<OpenAIRealtimeOptions> _options;

    public OpenAIRealtimeClient(
        HttpClient httpClient,
        IOptions<OpenAIRealtimeOptions> options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<OpenAIRealtimeCallResult> CreateCallAsync(
        string sdpOffer,
        string safetyIdentifier,
        CancellationToken cancellationToken)
    {
        var options = _options.Value;
        var apiKey = options.ApiKey?.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new OpenAIRealtimeConfigurationException("OpenAI API key is not configured.");
        }

        var sessionJson = JsonSerializer.Serialize(CreateSessionRequest(options), JsonOptions);
        using var content = new MultipartFormDataContent();
        using var sdpContent = new StringContent(sdpOffer, Encoding.UTF8, "application/sdp");
        using var sessionContent = new StringContent(sessionJson, Encoding.UTF8, "application/json");
        content.Add(sdpContent, "sdp");
        content.Add(sessionContent, "session");

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{options.EffectiveBaseUrl}/realtime/calls")
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Headers.TryAddWithoutValidation("OpenAI-Safety-Identifier", safetyIdentifier);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new OpenAIRealtimeApiException((int)response.StatusCode, responseBody);
        }

        return new OpenAIRealtimeCallResult(responseBody);
    }

    private static OpenAIRealtimeSessionRequest CreateSessionRequest(OpenAIRealtimeOptions options)
    {
        return new OpenAIRealtimeSessionRequest(
            Type: "realtime",
            Model: options.EffectiveRealtimeModel,
            Instructions: options.EffectiveRealtimeInstructions,
            Audio: new OpenAIRealtimeAudioRequest(
                Input: new OpenAIRealtimeAudioInputRequest(
                    Transcription: new OpenAIRealtimeInputTranscriptionRequest(
                        Model: options.EffectiveRealtimeInputTranscriptionModel),
                    TurnDetection: new OpenAIRealtimeTurnDetectionRequest(
                        Type: "server_vad")),
                Output: new OpenAIRealtimeAudioOutputRequest(
                    Voice: options.EffectiveRealtimeVoice)));
    }

    private sealed record OpenAIRealtimeSessionRequest(
        string Type,
        string Model,
        string Instructions,
        OpenAIRealtimeAudioRequest Audio);

    private sealed record OpenAIRealtimeAudioRequest(
        OpenAIRealtimeAudioInputRequest Input,
        OpenAIRealtimeAudioOutputRequest Output);

    private sealed record OpenAIRealtimeAudioInputRequest(
        OpenAIRealtimeInputTranscriptionRequest Transcription,
        [property: JsonPropertyName("turn_detection")]
        OpenAIRealtimeTurnDetectionRequest TurnDetection);

    private sealed record OpenAIRealtimeInputTranscriptionRequest(string Model);

    private sealed record OpenAIRealtimeTurnDetectionRequest(string Type);

    private sealed record OpenAIRealtimeAudioOutputRequest(string Voice);
}
