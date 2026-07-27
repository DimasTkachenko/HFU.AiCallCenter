namespace Hfu.VoiceRegistration.Infrastructure.GeminiLive.Protocol;

using System.Text.Json.Serialization;

// --- ИСХОДЯЩИЕ СООБЩЕНИЯ (Client -> Server) ---

public class GeminiClientMessage
{
    [JsonPropertyName("setup")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GeminiSetup? Setup { get; set; }

    [JsonPropertyName("realtimeInput")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GeminiRealtimeInput? RealtimeInput { get; set; }

    [JsonPropertyName("clientContent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GeminiClientContent? ClientContent { get; set; }

    [JsonPropertyName("toolResponse")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GeminiToolResponse? ToolResponse { get; set; }
}

public class GeminiClientContent
{
    [JsonPropertyName("turns")]
    public List<GeminiTurn> Turns { get; set; } = new();

    [JsonPropertyName("turnComplete")]
    public bool TurnComplete { get; set; } = true;
}

public class GeminiTurn
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    [JsonPropertyName("parts")]
    public List<GeminiPart> Parts { get; set; } = new();
}

public class GeminiSetup
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "models/gemini-2.0-flash-exp";

    [JsonPropertyName("generationConfig")]
    public GeminiGenerationConfig GenerationConfig { get; set; } = new();

    [JsonPropertyName("systemInstruction")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GeminiContent? SystemInstruction { get; set; }

    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<object>? Tools { get; set; }
}

public class GeminiGenerationConfig
{
    [JsonPropertyName("responseModalities")]
    public string[] ResponseModalities { get; set; } = new[] { "AUDIO" };

    [JsonPropertyName("speechConfig")]
    public GeminiSpeechConfig SpeechConfig { get; set; } = new();
}

public class GeminiSpeechConfig
{
    [JsonPropertyName("voiceConfig")]
    public GeminiVoiceConfig VoiceConfig { get; set; } = new();
}

public class GeminiVoiceConfig
{
    [JsonPropertyName("prebuiltVoiceConfig")]
    public GeminiPrebuiltVoiceConfig PrebuiltVoiceConfig { get; set; } = new() { VoiceName = "Aoede" };
}

public class GeminiPrebuiltVoiceConfig
{
    [JsonPropertyName("voiceName")]
    public string VoiceName { get; set; } = "Aoede"; // Варианты: Aoede, Charon, Fenrir, Kore, Puck
}

public class GeminiRealtimeInput
{
    [JsonPropertyName("audio")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GeminiRealtimeAudioChunk? Audio { get; set; }
}

public class GeminiRealtimeAudioChunk
{
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = "audio/pcm;rate=16000";

    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty; // Base64 PCM 16kHz
}

public class GeminiContent
{
    [JsonPropertyName("parts")]
    public List<GeminiPart> Parts { get; set; } = new();
}

public class GeminiPart
{
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }
}

public class GeminiToolResponse
{
    [JsonPropertyName("functionResponses")]
    public List<GeminiFunctionResponse> FunctionResponses { get; set; } = new();
}

public class GeminiFunctionResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("response")]
    public object Response { get; set; } = new();
}

public class GeminiToolDeclaration
{
    [JsonPropertyName("functionDeclarations")]
    public List<GeminiFunctionDeclaration> FunctionDeclarations { get; set; } = new();
}

public class GeminiFunctionDeclaration
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Parameters { get; set; }
}

// --- ВХОДЯЩИЕ СООБЩЕНИЯ (Server -> Client) ---

public class GeminiServerMessage
{
    [JsonPropertyName("setupComplete")]
    public GeminiSetupComplete? SetupComplete { get; set; }

    [JsonPropertyName("serverContent")]
    public GeminiServerContent? ServerContent { get; set; }

    [JsonPropertyName("toolCall")]
    public GeminiToolCall? ToolCall { get; set; }

    [JsonPropertyName("error")]
    public GeminiServerError? Error { get; set; }
}

public class GeminiSetupComplete
{
}

public class GeminiServerError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

public class GeminiServerContent
{
    [JsonPropertyName("modelTurn")]
    public GeminiModelTurn? ModelTurn { get; set; }

    [JsonPropertyName("turnComplete")]
    public bool TurnComplete { get; set; }

    [JsonPropertyName("interrupted")]
    public bool Interrupted { get; set; }
}

public class GeminiModelTurn
{
    [JsonPropertyName("parts")]
    public List<GeminiModelPart>? Parts { get; set; }
}

public class GeminiModelPart
{
    [JsonPropertyName("inlineData")]
    public GeminiInlineData? InlineData { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class GeminiInlineData
{
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = string.Empty; // audio/pcm;rate=24000

    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty; // Base64 PCM 24kHz
}

public class GeminiToolCall
{
    [JsonPropertyName("functionCalls")]
    public List<GeminiFunctionCall> FunctionCalls { get; set; } = new();
}

public class GeminiFunctionCall
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("args")]
    public System.Text.Json.Nodes.JsonObject? Args { get; set; }
}
