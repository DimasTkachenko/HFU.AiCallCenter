using Hfu.VoiceRegistration.Application.RegistrationCompletion;

namespace Hfu.VoiceRegistration.Infrastructure.RegistrationCompletion;

public sealed class InMemoryDemoRegistrationIdGenerator : IRegistrationIdGenerator
{
    private int _counter;

    public string Generate(DateTimeOffset now)
    {
        var value = Interlocked.Increment(ref _counter);
        return $"DEMO-{now:yyyy}-{value:000000}";
    }
}
