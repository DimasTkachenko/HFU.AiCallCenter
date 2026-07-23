namespace Hfu.VoiceRegistration.Api.OpenAIRealtime;

public sealed class OpenAIRealtimeOptions
{
    public const string SectionName = "OpenAI";

    private const string DefaultBaseUrl = "https://api.openai.com/v1";
    private const string DefaultRealtimeModel = "gpt-realtime-2.1";
    private const string DefaultRealtimeVoice = "marin";
    private const string DefaultRealtimeInputTranscriptionModel = "gpt-realtime-whisper";
    private const int DefaultRealtimeMaxSdpOfferCharacters = 131_072;
    private const int DefaultRealtimeCallsPerMinute = 12;
    private const double DefaultRealtimeServerVadThreshold = 0.5d;
    private const int DefaultRealtimeServerVadPrefixPaddingMs = 300;
    private const int DefaultRealtimeServerVadSilenceDurationMs = 700;
    private const int DefaultRealtimeServerVadIdleTimeoutMs = 4_000;
    public string? ApiKey { get; set; }

    public string? BaseUrl { get; set; }

    public string? RealtimeModel { get; set; }

    public string? RealtimeVoice { get; set; }

    public string? RealtimeInputTranscriptionModel { get; set; }

    public int? RealtimeMaxSdpOfferCharacters { get; set; }

    public int? RealtimeCallsPerMinute { get; set; }

    public string? RealtimeInstructions { get; set; }

    public double? RealtimeServerVadThreshold { get; set; }

    public int? RealtimeServerVadPrefixPaddingMs { get; set; }

    public int? RealtimeServerVadSilenceDurationMs { get; set; }

    public int? RealtimeServerVadIdleTimeoutMs { get; set; }

    public string EffectiveBaseUrl => ValueOrDefault(BaseUrl, DefaultBaseUrl).TrimEnd('/');

    public string EffectiveRealtimeModel => ValueOrDefault(RealtimeModel, DefaultRealtimeModel);

    public string EffectiveRealtimeVoice => ValueOrDefault(RealtimeVoice, DefaultRealtimeVoice);

    public string EffectiveRealtimeInputTranscriptionModel =>
        ValueOrDefault(RealtimeInputTranscriptionModel, DefaultRealtimeInputTranscriptionModel);

    public int EffectiveRealtimeMaxSdpOfferCharacters =>
        RealtimeMaxSdpOfferCharacters is > 0
            ? RealtimeMaxSdpOfferCharacters.Value
            : DefaultRealtimeMaxSdpOfferCharacters;

    public int EffectiveRealtimeCallsPerMinute =>
        RealtimeCallsPerMinute is > 0
            ? RealtimeCallsPerMinute.Value
            : DefaultRealtimeCallsPerMinute;

    public string EffectiveRealtimeInstructions =>
        ValueOrDefault(RealtimeInstructions, OpenAIRealtimeRegistrationPrompt.CurrentInstructions);

    public double EffectiveRealtimeServerVadThreshold =>
        RealtimeServerVadThreshold is > 0 and <= 1
            ? RealtimeServerVadThreshold.Value
            : DefaultRealtimeServerVadThreshold;

    public int EffectiveRealtimeServerVadPrefixPaddingMs =>
        RealtimeServerVadPrefixPaddingMs is >= 0
            ? RealtimeServerVadPrefixPaddingMs.Value
            : DefaultRealtimeServerVadPrefixPaddingMs;

    public int EffectiveRealtimeServerVadSilenceDurationMs =>
        RealtimeServerVadSilenceDurationMs is > 0
            ? RealtimeServerVadSilenceDurationMs.Value
            : DefaultRealtimeServerVadSilenceDurationMs;

    public int EffectiveRealtimeServerVadIdleTimeoutMs =>
        RealtimeServerVadIdleTimeoutMs is > 0
            ? RealtimeServerVadIdleTimeoutMs.Value
            : DefaultRealtimeServerVadIdleTimeoutMs;

    private static string ValueOrDefault(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();
    }
}
