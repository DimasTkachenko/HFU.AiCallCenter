namespace Hfu.VoiceRegistration.Application.ReferenceData;

public sealed record RegionResolutionResult(
    RegionResolutionStatus Status,
    RegionReferenceItem? Region,
    IReadOnlyList<RegionReferenceItem> Suggestions)
{
    public static RegionResolutionResult Resolved(RegionReferenceItem region)
    {
        ArgumentNullException.ThrowIfNull(region);
        return new RegionResolutionResult(
            RegionResolutionStatus.Resolved,
            region,
            Array.Empty<RegionReferenceItem>());
    }

    public static RegionResolutionResult Ambiguous(IReadOnlyList<RegionReferenceItem> suggestions)
    {
        ArgumentNullException.ThrowIfNull(suggestions);
        return new RegionResolutionResult(
            RegionResolutionStatus.Ambiguous,
            null,
            suggestions);
    }

    public static RegionResolutionResult NotFound()
    {
        return new RegionResolutionResult(
            RegionResolutionStatus.NotFound,
            null,
            Array.Empty<RegionReferenceItem>());
    }
}
