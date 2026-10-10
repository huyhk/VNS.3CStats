using VNS.ThreeCStats.Application.Ingestion;

namespace VNS.ThreeCStats.Web.Areas.Admin.Models;

public sealed class CebIngestionViewModel
{
    public string EventUrl { get; init; } = string.Empty;
    public string ExternalId { get; init; } = string.Empty;
    public long? SourceDocumentId { get; init; }
    public DateTimeOffset? RetrievedAt { get; init; }
    public string? ContentHash { get; init; }
    public string? ParserVersion { get; init; }
    public IReadOnlyList<ParsedMatch> Matches { get; init; } = Array.Empty<ParsedMatch>();
}
