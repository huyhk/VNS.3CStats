namespace VNS.ThreeCStats.Domain.Players;

public sealed class Player
{
    public long Id { get; set; }
    public required string CanonicalName { get; set; }
    public string? CountryCode { get; set; }
    public DateOnly? BirthDate { get; set; }

    public ICollection<PlayerAlias> Aliases { get; set; } = new List<PlayerAlias>();
}

public sealed class PlayerAlias
{
    public long Id { get; set; }
    public long PlayerId { get; set; }
    public required string Name { get; set; }
    public required string NormalizedName { get; set; }
    public string? Language { get; set; }
    public long? DataSourceId { get; set; }

    public Player Player { get; set; } = null!;
}
