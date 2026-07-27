namespace Hfu.VoiceRegistration.Api.Contracts;

public sealed record ConversationEventResponse(
    string Type,
    string Message,
    DateTimeOffset OccurredAt);
