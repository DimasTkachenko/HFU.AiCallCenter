namespace Hfu.VoiceRegistration.Application.ReferenceData;

public sealed record RegionReferenceItem(
    string Id,
    string Name,
    IReadOnlyList<string> Aliases);
