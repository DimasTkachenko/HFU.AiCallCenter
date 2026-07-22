using Hfu.VoiceRegistration.Application.Conversations;
using Hfu.VoiceRegistration.Infrastructure;
using Hfu.VoiceRegistration.Infrastructure.Conversations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Hfu.VoiceRegistration.Infrastructure.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructureRegistersConversationSessionStoreAndCleanupService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConversationSessions:IncompleteSessionExpiration"] = "00:30:00",
                ["ConversationSessions:CompletedSessionExpiration"] = "01:00:00",
                ["ConversationSessions:CleanupInterval"] = "00:05:00"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        var store = provider.GetRequiredService<IConversationSessionStore>();
        var timeProvider = provider.GetRequiredService<TimeProvider>();
        var options = provider.GetRequiredService<IOptions<ConversationSessionStoreOptions>>().Value;
        var hostedServices = provider.GetServices<IHostedService>();

        Assert.IsType<InMemoryConversationSessionStore>(store);
        Assert.Same(TimeProvider.System, timeProvider);
        Assert.Equal(TimeSpan.FromMinutes(30), options.IncompleteSessionExpiration);
        Assert.Equal(TimeSpan.FromMinutes(60), options.CompletedSessionExpiration);
        Assert.Equal(TimeSpan.FromMinutes(5), options.CleanupInterval);
        Assert.Contains(hostedServices, service => service is ConversationSessionCleanupService);
    }
}
