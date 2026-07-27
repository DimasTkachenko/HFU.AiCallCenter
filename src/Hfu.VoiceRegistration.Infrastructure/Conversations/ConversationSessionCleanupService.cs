using Hfu.VoiceRegistration.Application.Conversations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Hfu.VoiceRegistration.Infrastructure.Conversations;

public sealed class ConversationSessionCleanupService : BackgroundService
{
    private readonly IConversationSessionStore _sessionStore;
    private readonly ConversationSessionStoreOptions _options;
    private readonly TimeProvider _timeProvider;

    public ConversationSessionCleanupService(
        IConversationSessionStore sessionStore,
        IOptions<ConversationSessionStoreOptions> options,
        TimeProvider timeProvider)
    {
        _sessionStore = sessionStore;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.CleanupInterval, _timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await _sessionStore.CleanupExpiredAsync(stoppingToken);
        }
    }
}
