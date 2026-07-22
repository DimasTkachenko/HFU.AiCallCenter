namespace Hfu.VoiceRegistration.Application.ReferenceData;

public interface IRegionResolver
{
    RegionResolutionResult Resolve(string? value);
}
