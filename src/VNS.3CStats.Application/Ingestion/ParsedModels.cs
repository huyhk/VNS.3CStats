namespace VNS.ThreeCStats.Application.Ingestion;

public sealed record ParsedPlayer(
    string SourceName,
    string? ExternalPlayerId = null,
    string? CountryCode = null);

public sealed record ParsedMatch(
    DateTimeOffset? PlayedAt,
    int? MatchNumber,
    int? TableNumber,
    string? Stage,
    string? Group,
    ParsedPlayer Player1,
    ParsedPlayer Player2,
    int? Player1MatchPoints,
    int? Player2MatchPoints,
    int Player1Score,
    int Player2Score,
    int? Innings,
    decimal? Player1Average,
    decimal? Player2Average,
    int? Player1HighRun,
    int? Player2HighRun);

public sealed record ParsedTournament(
    string SourceCode,
    string? ExternalId,
    string Name,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? CountryCode,
    string? City,
    IReadOnlyList<ParsedMatch> Matches);
