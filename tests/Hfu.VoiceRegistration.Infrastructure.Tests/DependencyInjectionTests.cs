using Hfu.VoiceRegistration.Application.Conversations;
using Hfu.VoiceRegistration.Application.RegistrationCompletion;
using Hfu.VoiceRegistration.Infrastructure;
using Hfu.VoiceRegistration.Infrastructure.Conversations;
using Hfu.VoiceRegistration.Infrastructure.RegistrationCompletion;
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
        var registrationIdGenerator = provider.GetRequiredService<IRegistrationIdGenerator>();
        var fakeHfuService = provider.GetRequiredService<IFakeHfuRegistrationService>();
        var timeProvider = provider.GetRequiredService<TimeProvider>();
        var options = provider.GetRequiredService<IOptions<ConversationSessionStoreOptions>>().Value;
        var hostedServices = provider.GetServices<IHostedService>();

        Assert.IsType<InMemoryConversationSessionStore>(store);
        Assert.IsType<InMemoryDemoRegistrationIdGenerator>(registrationIdGenerator);
        Assert.IsType<FakeHfuRegistrationService>(fakeHfuService);
        Assert.Same(TimeProvider.System, timeProvider);
        Assert.Equal(TimeSpan.FromMinutes(30), options.IncompleteSessionExpiration);
        Assert.Equal(TimeSpan.FromMinutes(60), options.CompletedSessionExpiration);
        Assert.Equal(TimeSpan.FromMinutes(5), options.CleanupInterval);
        Assert.Contains(hostedServices, service => service is ConversationSessionCleanupService);
    }

    [Fact]
    public void FormatPostgresConnectionString_FormatsNeonUriCorrectly()
    {
        var uriString = "postgresql://neondb_owner:npg_xFACIL19Tbra@ep-orange-bar-ayxlne1x.c-5.us-east-2.aws.neon.tech/neondb?sslmode=require";
        var formatted = DependencyInjection.FormatPostgresConnectionString(uriString);

        Assert.Contains("Host=ep-orange-bar-ayxlne1x.c-5.us-east-2.aws.neon.tech", formatted);
        Assert.Contains("Database=neondb", formatted);
        Assert.Contains("Username=neondb_owner", formatted);
        Assert.Contains("Password=npg_xFACIL19Tbra", formatted);
    }
}
