using Hfu.VoiceRegistration.Application.Conversations;
using Hfu.VoiceRegistration.Domain.Conversations;
using Hfu.VoiceRegistration.Infrastructure.Conversations;
using Microsoft.Extensions.Options;

namespace Hfu.VoiceRegistration.Infrastructure.Tests.Conversations;

public sealed class InMemoryConversationSessionStoreTests
{
    [Fact]
    public async Task CreateAndGetReturnsStoredSession()
    {
        var now = new DateTimeOffset(2026, 7, 22, 16, 0, 0, TimeSpan.Zero);
        var store = CreateStore(now);
        var session = ConversationSession.Create(now);

        await store.CreateAsync(session, CancellationToken.None);

        var stored = await store.GetAsync(session.SessionId, CancellationToken.None);

        Assert.Equal(session, stored);
    }

    [Fact]
    public async Task CreateRejectsDuplicateSessionId()
    {
        var now = new DateTimeOffset(2026, 7, 22, 16, 0, 0, TimeSpan.Zero);
        var store = CreateStore(now);
        var session = ConversationSession.Create(now);

        await store.CreateAsync(session, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => store.CreateAsync(session, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateRejectsUnknownSessionId()
    {
        var now = new DateTimeOffset(2026, 7, 22, 16, 0, 0, TimeSpan.Zero);
        var store = CreateStore(now);
        var session = ConversationSession.Create(now);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => store.UpdateAsync(session, CancellationToken.None));
    }

    [Fact]
    public async Task RemoveDeletesStoredSession()
    {
        var now = new DateTimeOffset(2026, 7, 22, 16, 0, 0, TimeSpan.Zero);
        var store = CreateStore(now);
        var session = ConversationSession.Create(now);

        await store.CreateAsync(session, CancellationToken.None);
        await store.RemoveAsync(session.SessionId, CancellationToken.None);

        var stored = await store.GetAsync(session.SessionId, CancellationToken.None);
        Assert.Null(stored);
    }

    [Fact]
    public async Task SuccessfulMutationIncreasesVersionAndStoresEvents()
    {
        var now = new DateTimeOffset(2026, 7, 22, 16, 0, 0, TimeSpan.Zero);
        var store = CreateStore(now);
        var session = ConversationSession.Create(now);
        await store.CreateAsync(session, CancellationToken.None);

        var updated = await store.UpdateAsync(
            session.SessionId,
            static (current, _) =>
            {
                var updatedSession = current with
                {
                    Status = ConversationSessionStatus.Active,
                    LastActivityAt = current.LastActivityAt.AddMinutes(1)
                };

                return Task.FromResult(updatedSession.RecordEvent(
                    "SessionActivated",
                    "Conversation became active.",
                    updatedSession.LastActivityAt));
            },
            CancellationToken.None);

        var stored = await store.GetAsync(session.SessionId, CancellationToken.None);

        Assert.Equal(1, updated.Version);
        Assert.Equal(ConversationSessionStatus.Active, updated.Status);
        Assert.Single(updated.Events);
        Assert.Equal(updated, stored);
    }

    [Fact]
    public async Task MutationsForOneSessionRunOneAtATime()
    {
        var now = new DateTimeOffset(2026, 7, 22, 16, 0, 0, TimeSpan.Zero);
        var store = CreateStore(now);
        var session = ConversationSession.Create(now);
        await store.CreateAsync(session, CancellationToken.None);

        var insideMutation = 0;
        var maxInsideMutation = 0;
        var tasks = Enumerable.Range(1, 12)
            .Select(index => store.UpdateAsync(
                session.SessionId,
                async (current, cancellationToken) =>
                {
                    var currentInside = Interlocked.Increment(ref insideMutation);
                    UpdateMax(ref maxInsideMutation, currentInside);

                    await Task.Delay(10, cancellationToken);

                    Interlocked.Decrement(ref insideMutation);
                    return current.RecordEvent(
                        "Mutation",
                        $"Mutation {index}",
                        current.LastActivityAt.AddSeconds(index));
                },
                CancellationToken.None))
            .ToArray();

        await Task.WhenAll(tasks);
        var stored = await store.GetAsync(session.SessionId, CancellationToken.None);

        Assert.Equal(1, maxInsideMutation);
        Assert.NotNull(stored);
        Assert.Equal(12, stored.Version);
        Assert.Equal(12, stored.Events.Count);
    }

    [Fact]
    public async Task CleanupRemovesExpiredIncompleteSessions()
    {
        var now = new DateTimeOffset(2026, 7, 22, 16, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        var store = CreateStore(
            timeProvider,
            new ConversationSessionStoreOptions
            {
                IncompleteSessionExpiration = TimeSpan.FromMinutes(30),
                CompletedSessionExpiration = TimeSpan.FromMinutes(60)
            });

        var expired = ConversationSession.Create(now.AddMinutes(-31)) with
        {
            LastActivityAt = now.AddMinutes(-31)
        };
        var active = ConversationSession.Create(now.AddMinutes(-29)) with
        {
            LastActivityAt = now.AddMinutes(-29)
        };

        await store.CreateAsync(expired, CancellationToken.None);
        await store.CreateAsync(active, CancellationToken.None);

        var removedCount = await store.CleanupExpiredAsync(CancellationToken.None);

        Assert.Equal(1, removedCount);
        Assert.Null(await store.GetAsync(expired.SessionId, CancellationToken.None));
        Assert.NotNull(await store.GetAsync(active.SessionId, CancellationToken.None));
    }

    [Fact]
    public async Task CleanupRemovesExpiredCompletedSessions()
    {
        var now = new DateTimeOffset(2026, 7, 22, 16, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        var store = CreateStore(
            timeProvider,
            new ConversationSessionStoreOptions
            {
                IncompleteSessionExpiration = TimeSpan.FromMinutes(30),
                CompletedSessionExpiration = TimeSpan.FromMinutes(60)
            });

        var expired = ConversationSession.Create(now.AddMinutes(-61))
            .MarkCompleted(new RegistrationResult("HFU-DEMO-OLD", now.AddMinutes(-61)));
        var active = ConversationSession.Create(now.AddMinutes(-59))
            .MarkCompleted(new RegistrationResult("HFU-DEMO-NEW", now.AddMinutes(-59)));

        await store.CreateAsync(expired, CancellationToken.None);
        await store.CreateAsync(active, CancellationToken.None);

        var removedCount = await store.CleanupExpiredAsync(CancellationToken.None);

        Assert.Equal(1, removedCount);
        Assert.Null(await store.GetAsync(expired.SessionId, CancellationToken.None));
        Assert.NotNull(await store.GetAsync(active.SessionId, CancellationToken.None));
    }

    private static InMemoryConversationSessionStore CreateStore(DateTimeOffset now)
    {
        return CreateStore(new FakeTimeProvider(now), new ConversationSessionStoreOptions());
    }

    private static InMemoryConversationSessionStore CreateStore(
        TimeProvider timeProvider,
        ConversationSessionStoreOptions options)
    {
        return new InMemoryConversationSessionStore(Options.Create(options), timeProvider);
    }

    private static void UpdateMax(ref int target, int value)
    {
        var current = target;
        while (value > current)
        {
            var original = Interlocked.CompareExchange(ref target, value, current);
            if (original == current)
            {
                return;
            }

            current = original;
        }
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FakeTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}
