using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hfu.VoiceRegistration.Domain.Registration;
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
                    Voice: options.EffectiveRealtimeVoice)),
            Tools: CreateRegistrationTools(),
            ToolChoice: "auto");
    }

    private sealed record OpenAIRealtimeSessionRequest(
        string Type,
        string Model,
        string Instructions,
        OpenAIRealtimeAudioRequest Audio,
        IReadOnlyList<OpenAIRealtimeFunctionTool> Tools,
        [property: JsonPropertyName("tool_choice")]
        string ToolChoice);

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

    private sealed record OpenAIRealtimeFunctionTool(
        string Type,
        string Name,
        string Description,
        object Parameters);

    private static IReadOnlyList<OpenAIRealtimeFunctionTool> CreateRegistrationTools()
    {
        return
        [
            new(
                Type: "function",
                Name: "update_registration_fields",
                Description: "Save captured HFU registration field values in the backend draft. Use Ukrainian region names when saving regions.",
                Parameters: ObjectSchema(
                    new Dictionary<string, object?>
                    {
                        ["fields"] = new Dictionary<string, object?>
                        {
                            ["type"] = "array",
                            ["description"] = "Registration fields to save.",
                            ["minItems"] = 1,
                            ["items"] = ObjectSchema(
                                new Dictionary<string, object?>
                                {
                                    ["name"] = EnumString(
                                        "Backend registration field name.",
                                        RegistrationFieldNames.FirstName,
                                        RegistrationFieldNames.LastName,
                                        RegistrationFieldNames.Patronymic,
                                        RegistrationFieldNames.DateOfBirth,
                                        RegistrationFieldNames.PhoneNumber,
                                        RegistrationFieldNames.Email,
                                        RegistrationFieldNames.CurrentRegion,
                                        RegistrationFieldNames.CurrentCity,
                                        RegistrationFieldNames.ActualAddress,
                                        RegistrationFieldNames.UserCategory,
                                        RegistrationFieldNames.RegionBeforeWar,
                                        RegistrationFieldNames.DisplacedCertificateYear),
                                    ["value"] = new Dictionary<string, object?>
                                    {
                                        ["description"] = "Captured field value. Use ISO yyyy-MM-dd for dateOfBirth and a number for displacedCertificateYear.",
                                        ["type"] = new[] { "string", "number", "boolean" }
                                    },
                                    ["rawValue"] = StringSchema("Original user wording when it differs from the normalized value.")
                                },
                                "name",
                                "value")
                        }
                    },
                    "fields")),
            new(
                Type: "function",
                Name: "confirm_registration_fields",
                Description: "Mark captured registration fields as explicitly confirmed by the user.",
                Parameters: FieldNamesSchema()),
            new(
                Type: "function",
                Name: "mark_fields_for_clarification",
                Description: "Mark registration fields as needing clarification when the user answer is ambiguous or incomplete.",
                Parameters: ObjectSchema(
                    new Dictionary<string, object?>
                    {
                        ["fieldNames"] = FieldNamesArraySchema(),
                        ["reason"] = StringSchema("Short reason to show in diagnostics and state.")
                    },
                    "fieldNames")),
            new(
                Type: "function",
                Name: "clear_registration_fields",
                Description: "Clear incorrect registration fields from the backend draft.",
                Parameters: FieldNamesSchema()),
            new(
                Type: "function",
                Name: "get_registration_state",
                Description: "Read the current backend registration state, including missing fields, clarification needs, confirmation needs, and completion issues.",
                Parameters: ObjectSchema(new Dictionary<string, object?>())),
            new(
                Type: "function",
                Name: "complete_registration",
                Description: "Complete the demo registration only after the user gives personal data consent and final confirmation.",
                Parameters: ObjectSchema(
                    new Dictionary<string, object?>
                    {
                        ["personalDataConsent"] = BooleanSchema("True only when the user explicitly agrees to personal data processing."),
                        ["registrationConfirmed"] = BooleanSchema("True only when the user explicitly gives final registration confirmation.")
                    },
                    "personalDataConsent",
                    "registrationConfirmed"))
        ];
    }

    private static object FieldNamesSchema()
    {
        return ObjectSchema(
            new Dictionary<string, object?>
            {
                ["fieldNames"] = FieldNamesArraySchema()
            },
            "fieldNames");
    }

    private static object FieldNamesArraySchema()
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "array",
            ["description"] = "Backend registration field names.",
            ["minItems"] = 1,
            ["items"] = EnumString(
                "Backend registration field name.",
                RegistrationFieldNames.FirstName,
                RegistrationFieldNames.LastName,
                RegistrationFieldNames.Patronymic,
                RegistrationFieldNames.DateOfBirth,
                RegistrationFieldNames.PhoneNumber,
                RegistrationFieldNames.Email,
                RegistrationFieldNames.CurrentRegion,
                RegistrationFieldNames.CurrentCity,
                RegistrationFieldNames.ActualAddress,
                RegistrationFieldNames.UserCategory,
                RegistrationFieldNames.RegionBeforeWar,
                RegistrationFieldNames.DisplacedCertificateYear,
                RegistrationFieldNames.PersonalDataConsent,
                RegistrationFieldNames.RegistrationConfirmed)
        };
    }

    private static object ObjectSchema(
        Dictionary<string, object?> properties,
        params string[] required)
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["properties"] = properties,
            ["required"] = required
        };
    }

    private static object StringSchema(string description)
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "string",
            ["description"] = description
        };
    }

    private static object BooleanSchema(string description)
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "boolean",
            ["description"] = description
        };
    }

    private static object EnumString(string description, params string[] values)
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "string",
            ["description"] = description,
            ["enum"] = values
        };
    }
}
