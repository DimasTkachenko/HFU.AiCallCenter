namespace Hfu.VoiceRegistration.Application.ReferenceData;

public interface IRegionReferenceDataProvider
{
    IReadOnlyList<RegionReferenceItem> GetRegions();
}
