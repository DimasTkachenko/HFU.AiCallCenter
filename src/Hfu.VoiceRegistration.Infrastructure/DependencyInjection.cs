using Hfu.VoiceRegistration.Application.Conversations;
using Hfu.VoiceRegistration.Application.RegistrationCompletion;
using Hfu.VoiceRegistration.Infrastructure.Conversations;
using Hfu.VoiceRegistration.Infrastructure.RegistrationCompletion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hfu.VoiceRegistration.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<ConversationSessionStoreOptions>()
            .Bind(configuration.GetSection("ConversationSessions"));

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IConversationSessionStore, InMemoryConversationSessionStore>();
        services.AddSingleton<IRegistrationIdGenerator, InMemoryDemoRegistrationIdGenerator>();
        services.AddScoped<IFakeHfuRegistrationService, FakeHfuRegistrationService>();
        services.AddHostedService<ConversationSessionCleanupService>();

        return services;
    }
}
