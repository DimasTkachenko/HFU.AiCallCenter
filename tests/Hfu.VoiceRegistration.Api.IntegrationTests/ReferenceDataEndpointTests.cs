using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Hfu.VoiceRegistration.Api.IntegrationTests;

public sealed class ReferenceDataEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ReferenceDataEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetRegionsReturnsUkrainianReferenceData()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/reference-data/regions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = await ApiIntegrationTestHelpers.ReadJsonAsync(response);
        var regions = document.RootElement.GetProperty("regions");

        Assert.True(regions.GetArrayLength() >= 25);
        Assert.Contains(
            regions.EnumerateArray(),
            region => region.GetProperty("id").GetString() == "hfu-region-kharkivska"
                && region.GetProperty("name").GetString() == "Харківська область");
    }
}
