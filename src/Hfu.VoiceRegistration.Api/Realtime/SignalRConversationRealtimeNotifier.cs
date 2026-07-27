using Microsoft.AspNetCore.SignalR;

namespace Hfu.VoiceRegistration.Api.Realtime;

public sealed class SignalRConversationRealtimeNotifier : IConversationRealtimeNotifier
{
    private readonly IHubContext<ConversationHub, IConversationRealtimeClient> _hubContext;
    private readonly TimeProvider _timeProvider;

    public SignalRConversationRealtimeNotifier(
        IHubContext<ConversationHub, IConversationRealtimeClient> hubContext,
        TimeProvider timeProvider)
    {
        _hubContext = hubContext;
        _timeProvider = timeProvider;
    }

    public Task NotifyAsync(
        Guid sessionId,
        long version,
        ConversationRealtimeEventType type,
        string message,
        CancellationToken cancellationToken,
        string? correlationId = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var conversationEvent = new ConversationRealtimeEvent(
            Guid.NewGuid(),
            sessionId,
            version,
            type,
            message,
            _timeProvider.GetUtcNow(),
            correlationId);

        return _hubContext
            .Clients
            .Group(ConversationHubGroups.ForSession(sessionId))
            .ConversationEvent(conversationEvent);
    }
}
