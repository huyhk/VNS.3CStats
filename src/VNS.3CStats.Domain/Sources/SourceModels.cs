using VNS.ThreeCStats.Domain.Competitions;
using VNS.ThreeCStats.Domain.Players;

namespace VNS.ThreeCStats.Domain.Sources;

public enum VerificationStatus
{
    Unverified = 0,
    Secondary = 1,
    Verified = 2,
    Official = 3,
    Conflict = 4
}

public sealed class DataSource
{
    public long Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string BaseUrl { get; set; }
    public required string SourceType { get; set; }
}

public sealed class SourceDocument
{
    public long Id { get; set; }
    public long DataSourceId { get; set; }
    public string? ExternalId { get; set; }
    public required string Url { get; set; }
    public DateTimeOffset RetrievedAt { get; set; }
    public string? ContentType { get; set; }
    public required string ContentHash { get; set; }
    public string? RawContentPath { get; set; }
    public required string ParserVersion { get; set; }

    public DataSource DataSource { get; set; } = null!;
}

public sealed class MatchSource
{
    public long MatchId { get; set; }
    public long SourceDocumentId { get; set; }
    public string? ExternalMatchId { get; set; }
    public VerificationStatus VerificationStatus { get; set; }
    public string? SourceSnapshot { get; set; }

    public Match Match { get; set; } = null!;
    public SourceDocument SourceDocument { get; set; } = null!;
}

public sealed class SourcePlayerIdentity
{
    public long Id { get; set; }
    public long DataSourceId { get; set; }
    public required string ExternalPlayerId { get; set; }
    public required string SourceName { get; set; }
    public long PlayerId { get; set; }

    public DataSource DataSource { get; set; } = null!;
    public Player Player { get; set; } = null!;
}
