using System.Globalization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace VNS.ThreeCStats.Infrastructure.Sources.Ceb;

public sealed record DiscoveredCebEvent(
    string ExternalId,
    Uri Url,
    string Name,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? Discipline,
    string? Location);

public sealed partial class CebEventDiscoveryParser
{
    private static readonly Uri BaseUri = new("https://www.eurobillard.org/");

    public IReadOnlyList<DiscoveredCebEvent> Parse(string html)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(html);

        var document = new HtmlDocument();
        document.LoadHtml(html);

        var results = new Dictionary<string, DiscoveredCebEvent>(StringComparer.Ordinal);
        var links = document.DocumentNode.SelectNodes("//a[@href]") ?? [];

        foreach (var link in links)
        {
            var href = HtmlEntity.DeEntitize(link.GetAttributeValue("href", string.Empty)).Trim();
            if (!TryCreateEventUri(href, out var url, out var externalId))
            {
                continue;
            }

            var name = NormalizeText(link.InnerText);
            if (name.Length == 0)
            {
                continue;
            }

            var row = link.Ancestors("tr").FirstOrDefault();
            var cells = row?.Elements("td").ToArray() ?? [];
            var dates = row is null ? [] : DateRegex().Matches(NormalizeText(row.InnerText)).Select(x => ParseDate(x.Value)).ToArray();
            var discipline = cells.Length >= 4 ? NullIfEmpty(NormalizeText(cells[^4].InnerText)) : null;
            var location = cells.Length > 0 ? NullIfEmpty(NormalizeText(cells[^1].InnerText)) : null;

            results[externalId] = new DiscoveredCebEvent(
                externalId,
                url,
                name,
                dates.Length > 0 ? dates[0] : null,
                dates.Length > 1 ? dates[1] : dates.Length > 0 ? dates[0] : null,
                discipline,
                location);
        }

        return results.Values.ToArray();
    }

    private static bool TryCreateEventUri(string href, out Uri uri, out string externalId)
    {
        uri = null!;
        externalId = string.Empty;

        if (!Uri.TryCreate(BaseUri, href, out var candidate)
            || !candidate.Host.Equals(BaseUri.Host, StringComparison.OrdinalIgnoreCase)
            || !candidate.AbsolutePath.StartsWith("/events/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var match = EventIdRegex().Match(candidate.AbsolutePath);
        if (!match.Success)
        {
            return false;
        }

        uri = candidate;
        externalId = match.Groups["id"].Value;
        return true;
    }

    private static DateOnly ParseDate(string value) =>
        DateOnly.ParseExact(value, "dd-MM-yyyy", CultureInfo.InvariantCulture);

    private static string NormalizeText(string value) =>
        Regex.Replace(HtmlEntity.DeEntitize(value), @"\s+", " ").Trim();

    private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;

    [GeneratedRegex(@"-(?<id>\d+)\.html$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EventIdRegex();

    [GeneratedRegex(@"\b\d{2}-\d{2}-\d{4}\b", RegexOptions.CultureInvariant)]
    private static partial Regex DateRegex();
}
