namespace Hfu.VoiceRegistration.Api.Realtime;

public interface IConversationRealtimeNotifier
{
    Task NotifyAsync(
        Guid sessionId,
        long version,
        ConversationRealtimeEventType type,
        string message,
        CancellationToken cancellationToken,
        string? correlationId = null);
}
