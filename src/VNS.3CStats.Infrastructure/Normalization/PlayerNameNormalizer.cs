using System.Text;
using System.Text.RegularExpressions;

namespace VNS.ThreeCStats.Infrastructure.Normalization;

public static partial class PlayerNameNormalizer
{
    public static string Normalize(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        // Keep diacritics and scripts intact. Normalization is deliberately
        // conservative: punctuation variants and whitespace are safe to fold,
        // while accent removal or token reordering belongs in candidate matching.
        var normalized = name.Normalize(NormalizationForm.FormKC)
            .Replace('\u2018', '\'')
            .Replace('\u2019', '\'')
            .Replace('\u02BC', '\'')
            .Replace('\u0060', '\'')
            .ToUpperInvariant();

        return WhitespaceRegex().Replace(normalized, " ").Trim();
    }

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();
}
