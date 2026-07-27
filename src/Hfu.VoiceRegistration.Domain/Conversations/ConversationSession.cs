using Hfu.VoiceRegistration.Domain.Registration;

namespace Hfu.VoiceRegistration.Domain.Conversations;

public sealed record ConversationSession
{
    private ConversationSession(
        Guid sessionId,
        ConversationSessionStatus status,
        RegistrationDraft registrationDraft,
        long version,
        DateTimeOffset createdAt,
        DateTimeOffset lastActivityAt,
        string? realtimeConnectionId,
        RegistrationResult? registrationResult,
        IReadOnlyList<ConversationEvent> events)
    {
        SessionId = sessionId;
        Status = status;
        RegistrationDraft = registrationDraft;
        Version = version;
        CreatedAt = createdAt;
        LastActivityAt = lastActivityAt;
        RealtimeConnectionId = realtimeConnectionId;
        RegistrationResult = registrationResult;
        Events = events;
    }

    public Guid SessionId { get; init; }

    public ConversationSessionStatus Status { get; init; }

    public RegistrationDraft RegistrationDraft { get; init; }

    public long Version { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset LastActivityAt { get; init; }

    public string? RealtimeConnectionId { get; init; }

    public RegistrationResult? RegistrationResult { get; init; }

    public IReadOnlyList<ConversationEvent> Events { get; init; }

    public static ConversationSession Create(DateTimeOffset now)
    {
        return new ConversationSession(
            Guid.NewGuid(),
            ConversationSessionStatus.Created,
            RegistrationDraft.Create(),
            version: 0,
            createdAt: now,
            lastActivityAt: now,
            realtimeConnectionId: null,
            registrationResult: null,
            events: Array.Empty<ConversationEvent>());
    }

    public ConversationSession RecordEvent(string type, string message, DateTimeOffset occurredAt)
    {
        var events = Events
            .Append(new ConversationEvent(type, message, occurredAt))
            .ToArray();

        return this with
        {
            Version = Version + 1,
            LastActivityAt = occurredAt,
            Events = events
        };
    }

    public ConversationSession MarkCompleted(RegistrationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return this with
        {
            Status = ConversationSessionStatus.Completed,
            RegistrationResult = result,
            LastActivityAt = result.CompletedAt,
            Version = Version + 1
        };
    }
}
