using System.Globalization;
using System.Text;

namespace Hfu.VoiceRegistration.Application.ReferenceData;

public sealed class RegionResolver : IRegionResolver
{
    private readonly IReadOnlyList<IndexedRegion> _regions;

    public RegionResolver(IRegionReferenceDataProvider referenceDataProvider)
    {
        ArgumentNullException.ThrowIfNull(referenceDataProvider);

        _regions = referenceDataProvider
            .GetRegions()
            .Select(region => new IndexedRegion(
                region,
                region.Aliases
                    .Append(region.Name)
                    .Select(Normalize)
                    .Where(alias => !string.IsNullOrWhiteSpace(alias))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray()))
            .ToArray();
    }

    public RegionResolutionResult Resolve(string? value)
    {
        var normalized = Normalize(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return RegionResolutionResult.NotFound();
        }

        var exactMatches = _regions
            .Where(region => region.Aliases.Contains(normalized, StringComparer.Ordinal))
            .Select(region => region.Region)
            .ToArray();

        if (exactMatches.Length == 1)
        {
            return RegionResolutionResult.Resolved(exactMatches[0]);
        }

        if (exactMatches.Length > 1)
        {
            return RegionResolutionResult.Ambiguous(exactMatches);
        }

        var fuzzyMatches = _regions
            .Where(region => region.Aliases.Any(alias => FuzzyMatches(normalized, alias)))
            .Select(region => region.Region)
            .DistinctBy(region => region.Id)
            .ToArray();

        return fuzzyMatches.Length switch
        {
            0 => RegionResolutionResult.NotFound(),
            1 => RegionResolutionResult.Resolved(fuzzyMatches[0]),
            _ => RegionResolutionResult.Ambiguous(fuzzyMatches)
        };
    }

    private static bool FuzzyMatches(string query, string alias)
    {
        if (alias.StartsWith(query, StringComparison.Ordinal))
        {
            return true;
        }

        return alias
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Any(token => token.StartsWith(query, StringComparison.Ordinal));
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var character in value.Trim().Normalize(NormalizationForm.FormC))
        {
            var normalizedCharacter = character switch
            {
                'Ё' or 'ё' => 'е',
                'Ґ' => 'ґ',
                'І' => 'і',
                'Ї' => 'ї',
                'Є' => 'є',
                _ => char.ToLower(character, CultureInfo.InvariantCulture)
            };

            builder.Append(char.IsLetterOrDigit(normalizedCharacter)
                ? normalizedCharacter
                : ' ');
        }

        return string.Join(
            ' ',
            builder
                .ToString()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private sealed record IndexedRegion(
        RegionReferenceItem Region,
        IReadOnlyList<string> Aliases);
}
