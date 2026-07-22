namespace Hfu.VoiceRegistration.Application.Conversations;

public sealed class ConversationSessionStoreOptions
{
    public TimeSpan IncompleteSessionExpiration { get; init; } = TimeSpan.FromMinutes(30);

    public TimeSpan CompletedSessionExpiration { get; init; } = TimeSpan.FromMinutes(60);

    public TimeSpan CleanupInterval { get; init; } = TimeSpan.FromMinutes(5);
}
