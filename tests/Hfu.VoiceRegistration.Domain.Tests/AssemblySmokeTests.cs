using Hfu.VoiceRegistration.Domain;

namespace Hfu.VoiceRegistration.Domain.Tests;

public sealed class AssemblySmokeTests
{
    [Fact]
    public void DomainAssemblyHasExpectedName()
    {
        var assemblyName = typeof(DomainAssemblyMarker).Assembly.GetName();

        Assert.Equal("Hfu.VoiceRegistration.Domain", assemblyName.Name);
    }
}
