using Hfu.VoiceRegistration.Application.ReferenceData;
using Hfu.VoiceRegistration.Application.RegistrationTools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hfu.VoiceRegistration.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IRegionReferenceDataProvider, UkrainianRegionReferenceDataProvider>();
        services.AddSingleton<IRegionResolver, RegionResolver>();
        services.AddScoped<IRegistrationToolService, RegistrationToolService>();

        return services;
    }
}
