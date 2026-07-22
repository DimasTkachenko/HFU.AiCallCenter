using System.Collections.Concurrent;
using Hfu.VoiceRegistration.Application.Conversations;
using Hfu.VoiceRegistration.Domain.Conversations;
using Microsoft.Extensions.Options;

namespace Hfu.VoiceRegistration.Infrastructure.Conversations;

public sealed class InMemoryConversationSessionStore : IConversationSessionStore
{
    private readonly ConcurrentDictionary<Guid, StoredConversationSession> _sessions = new();
    private readonly ConversationSessionStoreOptions _options;
    private readonly TimeProvider _timeProvider;

    public InMemoryConversationSessionStore(
        IOptions<ConversationSessionStoreOptions> options,
        TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public async Task<ConversationSession?> GetAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_sessions.TryGetValue(sessionId, out var storedSession))
        {
            return null;
        }

        await storedSession.Gate.WaitAsync(cancellationToken);
        try
        {
            return storedSession.Session;
        }
        finally
        {
            storedSession.Gate.Release();
        }
    }

    public Task CreateAsync(
        ConversationSession session,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        var storedSession = new StoredConversationSession(session);
        if (!_sessions.TryAdd(session.SessionId, storedSession))
        {
            throw new InvalidOperationException($"Conversation session '{session.SessionId}' already exists.");
        }

        return Task.CompletedTask;
    }

    public async Task UpdateAsync(
        ConversationSession session,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!_sessions.TryGetValue(session.SessionId, out var storedSession))
        {
            throw new KeyNotFoundException($"Conversation session '{session.SessionId}' was not found.");
        }

        await storedSession.Gate.WaitAsync(cancellationToken);
        try
        {
            storedSession.Session = session;
        }
        finally
        {
            storedSession.Gate.Release();
        }
    }

    public async Task<ConversationSession> UpdateAsync(
        Guid sessionId,
        Func<ConversationSession, CancellationToken, Task<ConversationSession>> mutate,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mutate);

        if (!_sessions.TryGetValue(sessionId, out var storedSession))
        {
            throw new KeyNotFoundException($"Conversation session '{sessionId}' was not found.");
        }

        await storedSession.Gate.WaitAsync(cancellationToken);
        try
        {
            var current = storedSession.Session;
            var mutated = await mutate(current, cancellationToken);
            var versioned = EnsureVersionIncreased(current, mutated);
            storedSession.Session = versioned;

            return versioned;
        }
        finally
        {
            storedSession.Gate.Release();
        }
    }

    public async Task RemoveAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_sessions.TryRemove(sessionId, out var storedSession))
        {
            await storedSession.Gate.WaitAsync(cancellationToken);
            storedSession.Gate.Release();
        }
    }

    public async Task<int> CleanupExpiredAsync(CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var removedCount = 0;

        foreach (var (sessionId, storedSession) in _sessions.ToArray())
        {
            cancellationToken.ThrowIfCancellationRequested();

            await storedSession.Gate.WaitAsync(cancellationToken);
            try
            {
                if (!IsExpired(storedSession.Session, now))
                {
                    continue;
                }

                if (_sessions.TryRemove(sessionId, out _))
                {
                    removedCount++;
                }
            }
            finally
            {
                storedSession.Gate.Release();
            }
        }

        return removedCount;
    }

    private bool IsExpired(ConversationSession session, DateTimeOffset now)
    {
        var expiration = session.Status == ConversationSessionStatus.Completed
            ? _options.CompletedSessionExpiration
            : _options.IncompleteSessionExpiration;

        return now - session.LastActivityAt > expiration;
    }

    private static ConversationSession EnsureVersionIncreased(
        ConversationSession current,
        ConversationSession mutated)
    {
        if (mutated.Version > current.Version)
        {
            return mutated;
        }

        return mutated with { Version = current.Version + 1 };
    }

    private sealed class StoredConversationSession
    {
        public StoredConversationSession(ConversationSession session)
        {
            Session = session;
        }

        public ConversationSession Session { get; set; }

        public SemaphoreSlim Gate { get; } = new(1, 1);

    }
}
