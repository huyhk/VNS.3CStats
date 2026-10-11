using HtmlAgilityPack;
using VNS.ThreeCStats.Application.Ingestion;

namespace VNS.ThreeCStats.Infrastructure.Sources.Ceb;

public sealed class CebPlayerParser
{
    public IReadOnlyList<ParsedPlayer> ParsePlayers(string html)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(html);

        var document = new HtmlDocument();
        document.LoadHtml(html);

        var players = new List<ParsedPlayer>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // CEB event pages expose registered players as links to /players/{id}.html.
        // Read the source identity from the link instead of manufacturing an ID
        // from a normalized display name.
        var links = document.DocumentNode.SelectNodes("//a[contains(@href, '/players/')]");
        if (links is null)
        {
            return players;
        }

        foreach (var link in links)
        {
            var href = link.GetAttributeValue("href", string.Empty);
            var externalId = ReadExternalPlayerId(href);
            var sourceName = Text(link);

            if (string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(sourceName))
            {
                continue;
            }

            if (!seen.Add(externalId))
            {
                continue;
            }

            players.Add(new ParsedPlayer(
                sourceName,
                externalId,
                ReadCountryCode(link)));
        }

        return players;
    }

    private static string? ReadExternalPlayerId(string href)
    {
        const string marker = "/players/";
        var markerIndex = href.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return null;
        }

        var value = href[(markerIndex + marker.Length)..];
        var queryIndex = value.IndexOfAny(['?', '#']);
        if (queryIndex >= 0)
        {
            value = value[..queryIndex];
        }

        if (value.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            value = value[..^5];
        }

        return string.IsNullOrWhiteSpace(value) ? null : value.Trim('/');
    }

    private static string? ReadCountryCode(HtmlNode link)
    {
        // Prefer explicit ISO-like attributes when CEB provides them. Do not
        // infer a country from flag image filenames or unrelated page text.
        var node = link;
        for (var depth = 0; node is not null && depth < 4; depth++, node = node.ParentNode)
        {
            foreach (var attributeName in new[] { "data-country", "data-country-code", "data-nation" })
            {
                var value = node.GetAttributeValue(attributeName, string.Empty).Trim();
                if (value.Length == 3 && value.All(char.IsLetter))
                {
                    return value.ToUpperInvariant();
                }
            }
        }

        return null;
    }

    private static string Text(HtmlNode node) =>
        HtmlEntity.DeEntitize(node.InnerText).Trim();
}
