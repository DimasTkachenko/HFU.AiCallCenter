namespace Hfu.VoiceRegistration.Api.Contracts;

public sealed record RegionsResponse(
    IReadOnlyList<RegionResponse> Regions);

public sealed record RegionResponse(
    string Id,
    string Name,
    IReadOnlyList<string> Aliases);
