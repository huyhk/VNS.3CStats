using System.Globalization;
using HtmlAgilityPack;
using VNS.ThreeCStats.Application.Ingestion;

namespace VNS.ThreeCStats.Infrastructure.Sources.Ceb;

public sealed class CebMatchParser
{
    private const int ExpectedColumnCount = 11;

    public IReadOnlyList<ParsedMatch> ParseMatches(string html)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(html);

        var document = new HtmlDocument();
        document.LoadHtml(html);

        var candidates = document.DocumentNode.SelectNodes(
            "//*[contains(concat(' ', normalize-space(@class), ' '), ' box_ligne ')]") ?? [];

        var matches = new List<ParsedMatch>();
        foreach (var candidate in candidates)
        {
            if (!IsMatchRow(candidate))
            {
                continue;
            }

            matches.Add(ParseRowNode(candidate));
        }

        return matches;
    }

    public ParsedMatch ParseRow(string html)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(html);

        var document = new HtmlDocument();
        document.LoadHtml(html);

        var row = document.DocumentNode.SelectSingleNode(
            "//*[contains(concat(' ', normalize-space(@class), ' '), ' box_ligne ')]")
            ?? throw new FormatException("CEB match row was not found.");

        return ParseRowNode(row);
    }

    private static bool IsMatchRow(HtmlNode row)
    {
        var columns = row.SelectNodes("./div")?.ToArray();
        if (columns is null || columns.Length != ExpectedColumnCount)
        {
            return false;
        }

        return HasPair(columns[5]) &&
               HasPair(columns[6]) &&
               HasPair(columns[7]) &&
               HasPair(columns[8]) &&
               HasPair(columns[9]) &&
               HasPair(columns[10]);
    }

    private static ParsedMatch ParseRowNode(HtmlNode row)
    {
        var columns = row.SelectNodes("./div")?.ToArray() ?? [];
        if (columns.Length != ExpectedColumnCount)
        {
            throw new FormatException($"Expected {ExpectedColumnCount} CEB match columns but found {columns.Length}.");
        }

        var players = ReadPair(columns[5], ParseText);
        var matchPoints = ReadPair(columns[6], ParseNullableInt);
        var scores = ReadPair(columns[7], ParseRequiredInt);
        var innings = ReadPair(columns[8], ParseNullableInt);
        var averages = ReadPair(columns[9], ParseNullableDecimal);
        var highRuns = ReadPair(columns[10], ParseNullableInt);

        if (innings.Blue != innings.Red)
        {
            throw new FormatException("CEB row contains different innings values for the two players.");
        }

        return new ParsedMatch(
            ParseDateTime(Text(columns[0])),
            ParseNullableInt(Text(columns[1])),
            ParseNullableInt(Text(columns[2])),
            NullIfEmpty(Text(columns[3])),
            NullIfEmpty(Text(columns[4])),
            new ParsedPlayer(players.Blue),
            new ParsedPlayer(players.Red),
            matchPoints.Blue,
            matchPoints.Red,
            scores.Blue,
            scores.Red,
            innings.Blue,
            averages.Blue,
            averages.Red,
            highRuns.Blue,
            highRuns.Red);
    }

    private static bool HasPair(HtmlNode node) =>
        node.SelectSingleNode(".//span[contains(concat(' ', normalize-space(@class), ' '), ' bleu ')]") is not null &&
        node.SelectSingleNode(".//span[contains(concat(' ', normalize-space(@class), ' '), ' rouge ')]") is not null;

    private static Pair<T> ReadPair<T>(HtmlNode node, Func<string, T> parser)
    {
        var blue = node.SelectSingleNode(".//span[contains(concat(' ', normalize-space(@class), ' '), ' bleu ')]")
            ?? throw new FormatException("CEB blue value was not found.");
        var red = node.SelectSingleNode(".//span[contains(concat(' ', normalize-space(@class), ' '), ' rouge ')]")
            ?? throw new FormatException("CEB red value was not found.");

        return new Pair<T>(parser(Text(blue)), parser(Text(red)));
    }

    private static string ParseText(string value) => value;

    private static int ParseRequiredInt(string value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : throw new FormatException($"'{value}' is not a valid integer.");

    private static int? ParseNullableInt(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : ParseRequiredInt(value);

    private static decimal? ParseNullableDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result)
            ? result
            : throw new FormatException($"'{value}' is not a valid decimal.");
    }

    private static DateTimeOffset? ParseDateTime(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!DateTime.TryParseExact(
                value,
                "dd-MM-yyyy HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var result))
        {
            throw new FormatException($"'{value}' is not a valid CEB match date/time.");
        }

        return new DateTimeOffset(result, TimeSpan.Zero);
    }

    private static string Text(HtmlNode node) =>
        HtmlEntity.DeEntitize(node.InnerText).Trim();

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private sealed record Pair<T>(T Blue, T Red);
}
