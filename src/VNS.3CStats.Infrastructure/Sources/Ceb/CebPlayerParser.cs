using System.Text.RegularExpressions;
using HtmlAgilityPack;
using VNS.ThreeCStats.Application.Ingestion;

namespace VNS.ThreeCStats.Infrastructure.Sources.Ceb;

public sealed partial class CebPlayerParser
{
    public IReadOnlyList<ParsedPlayer> ParsePlayers(string html)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(html);

        var document = new HtmlDocument();
        document.LoadHtml(html);

        // The current CEB event page has no player profile links. The Players tab
        // is rendered as roster rows inside onglet_6. Its player column contains
        // values such as "KRISTIANSEN Daniel (DNK)".
        var roster = document.DocumentNode.SelectSingleNode("//*[@id='onglet_6']");
        if (roster is null)
        {
            return Array.Empty<ParsedPlayer>();
        }

        var rows = roster.SelectNodes(
            ".//*[contains(concat(' ', normalize-space(@class), ' '), ' box_ligne ')]");
        if (rows is null)
        {
            return Array.Empty<ParsedPlayer>();
        }

        var players = new List<ParsedPlayer>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var columns = row.SelectNodes("./div")?.ToArray();
            if (columns is null || columns.Length < 2 || IsHeaderRow(row))
            {
                continue;
            }

            var playerText = Text(columns[1]);
            var match = PlayerWithCountryRegex().Match(playerText);
            if (!match.Success)
            {
                continue;
            }

            var sourceName = match.Groups["name"].Value.Trim();
            var countryCode = NormalizeCountryCode(match.Groups["country"].Value);
            if (sourceName.Length == 0)
            {
                continue;
            }

            // This roster does not expose a stable CEB player ID. Position and
            // federation slot (e.g. FRA1) are event-specific and must not be used
            // as SourcePlayerIdentity.ExternalPlayerId.
            var key = $"{sourceName}\u001f{countryCode}";
            if (!seen.Add(key))
            {
                continue;
            }

            players.Add(new ParsedPlayer(sourceName, null, countryCode));
        }

        return players;
    }

    private static bool IsHeaderRow(HtmlNode row) =>
        row.GetAttributeValue("class", string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Contains("box_titres", StringComparer.OrdinalIgnoreCase);

    private static string NormalizeCountryCode(string value)
    {
        var code = value.Trim().ToUpperInvariant();

        // CEB is not fully consistent here: the same roster uses FRA/ITA/GRC
        // alongside FR/IT/GR. Normalize the observed two-letter variants to the
        // corresponding three-letter codes used elsewhere in the same event.
        return code switch
        {
            "FR" => "FRA",
            "IT" => "ITA",
            "GR" => "GRC",
            _ => code
        };
    }

    private static string Text(HtmlNode node) =>
        HtmlEntity.DeEntitize(node.InnerText).Trim();

    [GeneratedRegex(@"^(?<name>.+?)\s*\((?<country>[A-Za-z]{2,3})\)\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex PlayerWithCountryRegex();
}
