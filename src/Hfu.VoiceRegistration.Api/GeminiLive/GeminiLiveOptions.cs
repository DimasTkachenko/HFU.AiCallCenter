namespace Hfu.VoiceRegistration.Api.GeminiLive;

public sealed class GeminiLiveOptions
{
    public const string SectionName = "GeminiLive";

    private const string DefaultModel = "models/gemini-2.5-flash-native-audio-latest";
    private const string DefaultVoiceName = "Aoede";

    public string? ApiKey { get; set; }

    public string? Model { get; set; }

    public string? VoiceName { get; set; }

    public string EffectiveModel => ValueOrDefault(Model, DefaultModel);

    public string EffectiveVoiceName => ValueOrDefault(VoiceName, DefaultVoiceName);

    private static string ValueOrDefault(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();
    }
}