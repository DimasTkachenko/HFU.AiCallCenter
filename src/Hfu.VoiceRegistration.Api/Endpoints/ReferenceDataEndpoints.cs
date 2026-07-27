using Hfu.VoiceRegistration.Api.Contracts;
using Hfu.VoiceRegistration.Application.ReferenceData;

namespace Hfu.VoiceRegistration.Api.Endpoints;

public static class ReferenceDataEndpoints
{
    public static IEndpointRouteBuilder MapReferenceDataEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
                "/api/reference-data/regions",
                (IRegionReferenceDataProvider provider) =>
                    Results.Ok(ApiContractMapper.ToResponse(provider.GetRegions())))
            .WithName("GetRegions")
            .WithTags("Reference Data");

        return endpoints;
    }
}
