using Hfu.VoiceRegistration.Domain.Conversations;

namespace Hfu.VoiceRegistration.Application.Conversations;

public interface IConversationSessionStore
{
    Task<ConversationSession?> GetAsync(
        Guid sessionId,
        CancellationToken cancellationToken);

    Task CreateAsync(
        ConversationSession session,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        ConversationSession session,
        CancellationToken cancellationToken);

    Task<ConversationSession> UpdateAsync(
        Guid sessionId,
        Func<ConversationSession, CancellationToken, Task<ConversationSession>> mutate,
        CancellationToken cancellationToken);

    Task RemoveAsync(
        Guid sessionId,
        CancellationToken cancellationToken);

    Task<int> CleanupExpiredAsync(CancellationToken cancellationToken);
}
