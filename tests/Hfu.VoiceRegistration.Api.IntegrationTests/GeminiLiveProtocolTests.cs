using System.Text.Json;
using Hfu.VoiceRegistration.Infrastructure.GeminiLive;
using Hfu.VoiceRegistration.Infrastructure.GeminiLive.Protocol;

namespace Hfu.VoiceRegistration.Api.IntegrationTests;

public sealed class GeminiLiveProtocolTests
{
    [Fact]
    public void AudioChunkSerializesThroughRealtimeInputAudio()
    {
        var message = new GeminiClientMessage
        {
            RealtimeInput = new GeminiRealtimeInput
            {
                Audio = new GeminiRealtimeAudioChunk
                {
                    MimeType = "audio/pcm;rate=16000",
                    Data = "AQID"
                }
            }
        };

        var json = JsonSerializer.Serialize(message);

        using var document = JsonDocument.Parse(json);
        var realtimeInput = document.RootElement.GetProperty("realtimeInput");

        Assert.True(realtimeInput.TryGetProperty("audio", out var audio));
        Assert.Equal("audio/pcm;rate=16000", audio.GetProperty("mimeType").GetString());
        Assert.Equal("AQID", audio.GetProperty("data").GetString());
        Assert.False(realtimeInput.TryGetProperty("mediaChunks", out _));
    }

    [Fact]
    public void SetupCompleteDeserializesFromServerMessage()
    {
        var message = JsonSerializer.Deserialize<GeminiServerMessage>(
            """
            {
              "setupComplete": {}
            }
            """);

        Assert.NotNull(message);
        Assert.NotNull(message!.SetupComplete);
    }

    [Fact]
    public void InitialInterviewPromptAsksFirstQuestionWithoutStartupToolCall()
    {
        var prompt = GeminiLiveClient.BuildInitialInterviewPrompt();

        Assert.DoesNotContain("get_registration_state", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ім", prompt, StringComparison.OrdinalIgnoreCase);
    }
}
