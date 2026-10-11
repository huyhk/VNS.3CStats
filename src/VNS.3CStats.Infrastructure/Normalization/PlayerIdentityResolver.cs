using Microsoft.EntityFrameworkCore;
using VNS.ThreeCStats.Domain.Players;
using VNS.ThreeCStats.Domain.Sources;
using VNS.ThreeCStats.Infrastructure.Persistence;

namespace VNS.ThreeCStats.Infrastructure.Normalization;

public enum PlayerIdentityResolutionKind
{
    Unresolved = 0,
    SourceIdentity = 1,
    ExactNameAndCountry = 2,
    Ambiguous = 3
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

        // A real source ID is authoritative inside that source when one exists.
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
                return new PlayerIdentityResolution(
                    PlayerIdentityResolutionKind.SourceIdentity,
                    sourceIdentity.Player,
                    [sourceIdentity.Player]);
            }
        }

        // CEB and similar sources have no stable player ID. An already confirmed
        // source identity is therefore matched by the conservative source key.
        var mappedIdentities = await dbContext.SourcePlayerIdentities
            .AsNoTracking()
            .Include(x => x.Player)
            .Where(x => x.DataSourceId == dataSourceId
                && x.NormalizedName == normalizedName
                && x.CountryCode == normalizedCountryCode)
            .ToListAsync(cancellationToken);

        var mappedPlayers = mappedIdentities
            .Select(x => x.Player)
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .ToList();

        if (mappedPlayers.Count == 1)
        {
            return new PlayerIdentityResolution(
                PlayerIdentityResolutionKind.SourceIdentity,
                mappedPlayers[0],
                mappedPlayers);
        }

        if (mappedPlayers.Count > 1)
        {
            return new PlayerIdentityResolution(
                PlayerIdentityResolutionKind.Ambiguous,
                null,
                mappedPlayers);
        }

        // Only exact normalized aliases/canonical names with the same country are
        // safe enough to resolve automatically. Fuzzy variants remain candidates
        // for a later review workflow rather than being silently merged.
        var aliasPlayerIds = dbContext.PlayerAliases
            .Where(x => x.NormalizedName == normalizedName)
            .Select(x => x.PlayerId);

        var candidates = await dbContext.Players
            .AsNoTracking()
            .Where(x => x.CountryCode == normalizedCountryCode
                && (PlayerNameNormalizer.NormalizeForStorage(x.CanonicalName) == normalizedName
                    || aliasPlayerIds.Contains(x.Id)))
            .ToListAsync(cancellationToken);

        if (candidates.Count == 1)
        {
            return new PlayerIdentityResolution(
                PlayerIdentityResolutionKind.ExactNameAndCountry,
                candidates[0],
                candidates);
        }

        return new PlayerIdentityResolution(
            candidates.Count > 1
                ? PlayerIdentityResolutionKind.Ambiguous
                : PlayerIdentityResolutionKind.Unresolved,
            null,
            candidates);
    }

    private static string? NormalizeCountryCode(string? countryCode) =>
        string.IsNullOrWhiteSpace(countryCode)
            ? null
            : countryCode.Trim().ToUpperInvariant();
}
