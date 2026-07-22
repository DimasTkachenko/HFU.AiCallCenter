using Hfu.VoiceRegistration.Application;
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
}
