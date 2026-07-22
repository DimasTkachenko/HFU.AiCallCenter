using Hfu.VoiceRegistration.Infrastructure.RegistrationCompletion;

namespace Hfu.VoiceRegistration.Infrastructure.Tests.RegistrationCompletion;

public sealed class InMemoryDemoRegistrationIdGeneratorTests
{
    [Fact]
    public void GenerateCreatesSequentialDemoIdsForCurrentYear()
    {
        var generator = new InMemoryDemoRegistrationIdGenerator();
        var now = new DateTimeOffset(2026, 7, 22, 10, 0, 0, TimeSpan.Zero);

        var first = generator.Generate(now);
        var second = generator.Generate(now);

        Assert.Equal("DEMO-2026-000001", first);
        Assert.Equal("DEMO-2026-000002", second);
    }
}
