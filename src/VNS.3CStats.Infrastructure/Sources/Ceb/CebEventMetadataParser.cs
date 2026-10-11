using System.Globalization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace VNS.ThreeCStats.Infrastructure.Sources.Ceb;

public sealed record CebEventMetadata(
    string Name,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Venue,
    string? City,
    string? CountryName,
    string? Discipline);

public sealed partial class CebEventMetadataParser
{
    public CebEventMetadata Parse(string html)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(html);

        var document = new HtmlDocument();
        document.LoadHtml(html);

        var name = ReadEventName(document)
            ?? throw new FormatException("CEB event name was not found.");
        var eventDates = ReadLabelValue(document, "Event dates")
            ?? throw new FormatException("CEB event dates were not found.");
        var (startDate, endDate) = ParseEventDates(eventDates);

        return new CebEventMetadata(
            name,
            startDate,
            endDate,
            ReadLabelValue(document, "Location"),
            ReadLabelValue(document, "City"),
            ReadLabelValue(document, "Country"),
            ReadLabelValue(document, "Discipline"));
    }

    private static string? ReadEventName(HtmlDocument document)
    {
        var heading = document.DocumentNode.SelectSingleNode("//h1|//h2");
        return heading is null ? null : NullIfEmpty(Text(heading));
    }

    private static string? ReadLabelValue(HtmlDocument document, string label)
    {
        var textNode = document.DocumentNode
            .DescendantsAndSelf()
            .Where(x => x.NodeType == HtmlNodeType.Text)
            .FirstOrDefault(x => string.Equals(NormalizeText(x.InnerText), label, StringComparison.OrdinalIgnoreCase));

        if (textNode is null)
        {
            return null;
        }

        var labelContainer = textNode.ParentNode;
        var value = FindFollowingValue(labelContainer);
        return NullIfEmpty(value);
    }

    private static string? FindFollowingValue(HtmlNode labelContainer)
    {
        var current = labelContainer;
        while (current is not null)
        {
            var sibling = current.NextSibling;
            while (sibling is not null)
            {
                var text = NormalizeText(sibling.InnerText);
                if (text.Length > 0)
                {
                    return text;
                }

                sibling = sibling.NextSibling;
            }

            current = current.ParentNode;
            if (current?.NodeType == HtmlNodeType.Document)
            {
                break;
            }
        }

        return null;
    }

    private static (DateOnly StartDate, DateOnly? EndDate) ParseEventDates(string value)
    {
        var matches = EventDateRegex().Matches(value);
        if (matches.Count == 0)
        {
            throw new FormatException($"'{value}' does not contain a valid CEB event date.");
        }

        var startDate = ParseDate(matches[0].Groups["date"].Value);
        var endDate = matches.Count > 1
            ? ParseDate(matches[1].Groups["date"].Value)
            : startDate;

        return (startDate, endDate);
    }

    private static DateOnly ParseDate(string value) =>
        DateOnly.TryParseExact(value, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)
            ? result
            : throw new FormatException($"'{value}' is not a valid CEB event date.");

    private static string Text(HtmlNode node) => NormalizeText(HtmlEntity.DeEntitize(node.InnerText));

    private static string NormalizeText(string value) =>
        Regex.Replace(HtmlEntity.DeEntitize(value), @"\s+", " ").Trim();

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    [GeneratedRegex(@"(?<date>\d{2}-\d{2}-\d{4})(?:\s+\d{2}:\d{2})?", RegexOptions.CultureInvariant)]
    private static partial Regex EventDateRegex();
}
