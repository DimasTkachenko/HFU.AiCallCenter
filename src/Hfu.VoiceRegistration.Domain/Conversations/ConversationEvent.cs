namespace Hfu.VoiceRegistration.Domain.Conversations;

public sealed record ConversationEvent(
    string Type,
    string Message,
    DateTimeOffset OccurredAt);
