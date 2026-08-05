namespace Hfu.VoiceRegistration.Infrastructure.Persistence.Entities;

public sealed class SessionRecordEntity
{
    public Guid SessionId { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset LastActivityAt { get; set; }

    public int EventCount { get; set; }

    public string DraftJson { get; set; } = "{}";

    public string EventsJson { get; set; } = "[]";

    public string? DemoRegistrationId { get; set; }
}
