using Microsoft.EntityFrameworkCore;
using VNS.ThreeCStats.Domain.Players;
using VNS.ThreeCStats.Infrastructure.Persistence;

namespace VNS.ThreeCStats.Infrastructure.Normalization;

public enum PlayerIdentityResolutionKind
{
    Unresolved = 0,
    SourceIdentity = 1,
    ExactNameAndCountry = 2,
    Ambiguous = 3,
    Created = 4
}

public sealed record PlayerIdentityResolution(
    PlayerIdentityResolutionKind Kind,
    Player? Player,
    IReadOnlyList<Player> Candidates)
{
    public bool IsResolved => Player is not null;
}

public sealed class PlayerIdentityResolver(AppDbContext dbContext)
{
    public async Task<PlayerIdentityResolution> ResolveAsync(
        long dataSourceId,
        string? externalPlayerId,
        string sourceName,
        string? countryCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);

        var normalizedName = PlayerNameNormalizer.Normalize(sourceName);
        var normalizedCountryCode = NormalizeCountryCode(countryCode);

        if (!string.IsNullOrWhiteSpace(externalPlayerId))
        {
            var sourceIdentity = await dbContext.SourcePlayerIdentities
                .AsNoTracking()
                .Include(x => x.Player)
                .SingleOrDefaultAsync(
                    x => x.DataSourceId == dataSourceId
                        && x.ExternalPlayerId == externalPlayerId,
                    cancellationToken);

            if (sourceIdentity is not null)
            {
                return Resolved(PlayerIdentityResolutionKind.SourceIdentity, sourceIdentity.Player);
            }
        }

        var mappedPlayers = await dbContext.SourcePlayerIdentities
            .AsNoTracking()
            .Include(x => x.Player)
            .Where(x => x.DataSourceId == dataSourceId
                && x.NormalizedName == normalizedName
                && x.CountryCode == normalizedCountryCode)
            .Select(x => x.Player)
            .ToListAsync(cancellationToken);

        mappedPlayers = mappedPlayers
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .ToList();

        if (mappedPlayers.Count == 1)
        {
            return Resolved(PlayerIdentityResolutionKind.SourceIdentity, mappedPlayers[0]);
        }

        if (mappedPlayers.Count > 1)
        {
            return Ambiguous(mappedPlayers);
        }

        var aliasPlayerIds = dbContext.PlayerAliases
            .Where(x => x.NormalizedName == normalizedName)
            .Select(x => x.PlayerId);

        var aliasCandidates = await dbContext.Players
            .AsNoTracking()
            .Where(x => x.CountryCode == normalizedCountryCode
                && aliasPlayerIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var countryPlayers = await dbContext.Players
            .AsNoTracking()
            .Where(x => x.CountryCode == normalizedCountryCode)
            .ToListAsync(cancellationToken);

        var candidates = aliasCandidates
            .Concat(countryPlayers.Where(x => PlayerNameNormalizer.Normalize(x.CanonicalName) == normalizedName))
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .ToList();

        if (candidates.Count == 1)
        {
            return Resolved(PlayerIdentityResolutionKind.ExactNameAndCountry, candidates[0]);
        }

        return candidates.Count > 1
            ? Ambiguous(candidates)
            : new PlayerIdentityResolution(PlayerIdentityResolutionKind.Unresolved, null, []);
    }

    private static PlayerIdentityResolution Resolved(PlayerIdentityResolutionKind kind, Player player) =>
        new(kind, player, [player]);

    private static PlayerIdentityResolution Ambiguous(IReadOnlyList<Player> candidates) =>
        new(PlayerIdentityResolutionKind.Ambiguous, null, candidates);

    private static string? NormalizeCountryCode(string? countryCode) =>
        string.IsNullOrWhiteSpace(countryCode)
            ? null
            : countryCode.Trim().ToUpperInvariant();
}
