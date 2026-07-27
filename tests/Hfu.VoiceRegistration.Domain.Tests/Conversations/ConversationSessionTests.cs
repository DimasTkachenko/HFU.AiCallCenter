using Hfu.VoiceRegistration.Domain.Conversations;
using Hfu.VoiceRegistration.Domain.Registration;

namespace Hfu.VoiceRegistration.Domain.Tests.Conversations;

public sealed class ConversationSessionTests
{
    [Fact]
    public void CreateInitializesSessionWithDraftAndCreatedStatus()
    {
        var now = new DateTimeOffset(2026, 7, 22, 15, 0, 0, TimeSpan.Zero);

        var session = ConversationSession.Create(now);

        Assert.NotEqual(Guid.Empty, session.SessionId);
        Assert.Equal(ConversationSessionStatus.Created, session.Status);
        Assert.Equal(0, session.Version);
        Assert.Equal(now, session.CreatedAt);
        Assert.Equal(now, session.LastActivityAt);
        Assert.Null(session.RealtimeConnectionId);
        Assert.Null(session.RegistrationResult);
        Assert.Empty(session.Events);
        Assert.Equal(RegistrationFieldStatus.Missing, session.RegistrationDraft.FirstName.Status);
    }

    [Fact]
    public void RecordEventUpdatesActivityTimestampAndVersion()
    {
        var createdAt = new DateTimeOffset(2026, 7, 22, 15, 0, 0, TimeSpan.Zero);
        var eventAt = createdAt.AddMinutes(3);
        var session = ConversationSession.Create(createdAt);

        var updated = session.RecordEvent("RegistrationStarted", "User started the registration flow.", eventAt);

        Assert.Equal(1, updated.Version);
        Assert.Equal(eventAt, updated.LastActivityAt);
        var conversationEvent = Assert.Single(updated.Events);
        Assert.Equal("RegistrationStarted", conversationEvent.Type);
        Assert.Equal("User started the registration flow.", conversationEvent.Message);
        Assert.Equal(eventAt, conversationEvent.OccurredAt);
    }

    [Fact]
    public void MarkCompletedStoresDemoRegistrationResult()
    {
        var completedAt = new DateTimeOffset(2026, 7, 22, 16, 0, 0, TimeSpan.Zero);
        var session = ConversationSession.Create(completedAt.AddMinutes(-10));
        var result = new RegistrationResult("HFU-DEMO-0001", completedAt);

        var completed = session.MarkCompleted(result);

        Assert.Equal(ConversationSessionStatus.Completed, completed.Status);
        Assert.Equal(result, completed.RegistrationResult);
        Assert.Equal(completedAt, completed.LastActivityAt);
        Assert.Equal(1, completed.Version);
    }
}
