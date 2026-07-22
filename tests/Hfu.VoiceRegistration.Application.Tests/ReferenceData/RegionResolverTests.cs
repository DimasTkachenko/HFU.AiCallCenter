using Hfu.VoiceRegistration.Application.ReferenceData;

namespace Hfu.VoiceRegistration.Application.Tests.ReferenceData;

public sealed class RegionResolverTests
{
    [Theory]
    [InlineData("Харківська область")]
    [InlineData(" харьковская   область ")]
    [InlineData("Харківська")]
    public void ResolveMatchesUkrainianAndRussianAliasesToUkrainianCanonicalName(
        string value)
    {
        var resolver = CreateResolver();

        var result = resolver.Resolve(value);

        Assert.Equal(RegionResolutionStatus.Resolved, result.Status);
        Assert.NotNull(result.Region);
        Assert.Equal("hfu-region-kharkivska", result.Region.Id);
        Assert.Equal("Харківська область", result.Region.Name);
        Assert.Empty(result.Suggestions);
    }

    [Fact]
    public void ResolveReturnsAmbiguousResultWithUkrainianSuggestions()
    {
        var resolver = CreateResolver();

        var result = resolver.Resolve("Київ");

        Assert.Equal(RegionResolutionStatus.Ambiguous, result.Status);
        Assert.Null(result.Region);
        Assert.Contains(result.Suggestions, suggestion => suggestion.Name == "Київська область");
        Assert.Contains(result.Suggestions, suggestion => suggestion.Name == "м. Київ");
    }

    [Fact]
    public void ResolveDoesNotAcceptGeneratedRegionIdsFromModel()
    {
        var resolver = CreateResolver();

        var result = resolver.Resolve("hfu-region-kharkivska");

        Assert.Equal(RegionResolutionStatus.NotFound, result.Status);
        Assert.Null(result.Region);
    }

    [Fact]
    public void ReferenceDataExposesUkrainianRegionNames()
    {
        var provider = new UkrainianRegionReferenceDataProvider();

        var regions = provider.GetRegions();

        Assert.Contains(regions, region => region.Name == "Харківська область");
        Assert.Contains(regions, region => region.Name == "Київська область");
        Assert.Contains(regions, region => region.Name == "м. Київ");
        Assert.DoesNotContain(regions, region => region.Name == "Kharkiv region");
    }

    private static RegionResolver CreateResolver()
    {
        return new RegionResolver(new UkrainianRegionReferenceDataProvider());
    }
}
