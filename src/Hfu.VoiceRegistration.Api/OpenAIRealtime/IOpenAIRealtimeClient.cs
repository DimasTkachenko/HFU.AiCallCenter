namespace Hfu.VoiceRegistration.Api.OpenAIRealtime;

public interface IOpenAIRealtimeClient
{
    Task<OpenAIRealtimeCallResult> CreateCallAsync(
        string sdpOffer,
        string safetyIdentifier,
        CancellationToken cancellationToken);
}
