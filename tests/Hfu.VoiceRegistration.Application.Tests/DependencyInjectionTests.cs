using Hfu.VoiceRegistration.Application;
using Hfu.VoiceRegistration.Application.Conversations;
using Hfu.VoiceRegistration.Application.RegistrationTools;
using Hfu.VoiceRegistration.Domain.Conversations;
using Microsoft.Extensions.DependencyInjection;

namespace Hfu.VoiceRegistration.Application.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddApplicationReturnsTheSameServiceCollection()
    {
        var services = new ServiceCollection();

        var returned = services.AddApplication();

        Assert.Same(services, returned);
    }

    [Fact]
    public void AddApplicationRegistersRegistrationToolService()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConversationSessionStore, FakeConversationSessionStore>();
        services.AddApplication();

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IRegistrationToolService)
                && descriptor.ImplementationType == typeof(RegistrationToolService)
                && descriptor.Lifetime == ServiceLifetime.Scoped);
    }

    private sealed class FakeConversationSessionStore : IConversationSessionStore
    {
        public Task<ConversationSession?> GetAsync(
            Guid sessionId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<ConversationSession?>(null);
        }

        public Task CreateAsync(
            ConversationSession session,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            ConversationSession session,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<ConversationSession> UpdateAsync(
            Guid sessionId,
            Func<ConversationSession, CancellationToken, Task<ConversationSession>> mutate,
            CancellationToken cancellationToken)
        {
            throw new KeyNotFoundException();
        }

        public Task RemoveAsync(
            Guid sessionId,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<int> CleanupExpiredAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }
    }
}
