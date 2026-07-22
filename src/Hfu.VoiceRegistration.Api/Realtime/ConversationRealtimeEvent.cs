namespace Hfu.VoiceRegistration.Api.Realtime;

public sealed record ConversationRealtimeEvent(
    Guid EventId,
    Guid SessionId,
    long Version,
    ConversationRealtimeEventType Type,
    string Message,
    DateTimeOffset OccurredAtUtc,
    string? CorrelationId);
