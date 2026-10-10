using VNS.ThreeCStats.Domain.Players;

namespace VNS.ThreeCStats.Domain.Competitions;

public enum CompetitionType
{
    Other = 0,
    WorldCup = 1,
    WorldChampionship = 2,
    ContinentalChampionship = 3,
    NationalChampionship = 4,
    TeamChampionship = 5
}

public sealed class Organization
{
    public long Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
}

public sealed class Competition
{
    public long Id { get; set; }
    public long OrganizationId { get; set; }
    public required string Name { get; set; }
    public CompetitionType Type { get; set; }

    public Organization Organization { get; set; } = null!;
}

public sealed class Tournament
{
    public long Id { get; set; }
    public long CompetitionId { get; set; }
    public required string Name { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? CountryCode { get; set; }
    public string? City { get; set; }
    public string? SourceExternalId { get; set; }

    public Competition Competition { get; set; } = null!;
}

public sealed class Match
{
    public long Id { get; set; }
    public long TournamentId { get; set; }
    public DateTimeOffset? PlayedAt { get; set; }
    public string? Stage { get; set; }
    public string? Group { get; set; }
    public int? MatchNumber { get; set; }
    public int? TableNumber { get; set; }
    public long Player1Id { get; set; }
    public long Player2Id { get; set; }
    public int Player1Score { get; set; }
    public int Player2Score { get; set; }
    public int? Player1MatchPoints { get; set; }
    public int? Player2MatchPoints { get; set; }
    public int? Player1Innings { get; set; }
    public int? Player2Innings { get; set; }
    public decimal? Player1Average { get; set; }
    public decimal? Player2Average { get; set; }
    public int? Player1HighRun { get; set; }
    public int? Player2HighRun { get; set; }
    public long? WinnerId { get; set; }

    public Tournament Tournament { get; set; } = null!;
    public Player Player1 { get; set; } = null!;
    public Player Player2 { get; set; } = null!;
    public Player? Winner { get; set; }
}
