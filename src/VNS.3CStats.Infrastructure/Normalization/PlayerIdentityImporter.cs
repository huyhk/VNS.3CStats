using Microsoft.EntityFrameworkCore;
using VNS.ThreeCStats.Application.Ingestion;
using VNS.ThreeCStats.Domain.Players;
using VNS.ThreeCStats.Domain.Sources;
using VNS.ThreeCStats.Infrastructure.Persistence;

namespace VNS.ThreeCStats.Infrastructure.Normalization;

public sealed class PlayerIdentityImporter(
    AppDbContext dbContext,
    PlayerIdentityResolver resolver)
{
    public async Task<PlayerIdentityResolution> ImportAsync(
        long dataSourceId,
        ParsedPlayer parsedPlayer,
        CancellationToken cancellationToken = default)
    {
        var resolution = await resolver.ResolveAsync(
            dataSourceId,
            parsedPlayer.ExternalPlayerId,
            parsedPlayer.SourceName,
            parsedPlayer.CountryCode,
            cancellationToken);

        if (resolution.Kind == PlayerIdentityResolutionKind.Ambiguous)
        {
            return resolution;
        }

        var player = resolution.Player;
        if (player is null)
        {
            player = new Player
            {
                CanonicalName = parsedPlayer.SourceName.Trim(),
                CountryCode = NormalizeCountryCode(parsedPlayer.CountryCode)
            };
            dbContext.Players.Add(player);
        }

        await EnsureSourceIdentityAsync(dataSourceId, parsedPlayer, player, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new PlayerIdentityResolution(
            resolution.IsResolved
                ? resolution.Kind
                : PlayerIdentityResolutionKind.Created,
            player,
            [player]);
    }

    private async Task EnsureSourceIdentityAsync(
        long dataSourceId,
        ParsedPlayer parsedPlayer,
        Player player,
        CancellationToken cancellationToken)
    {
        var normalizedName = PlayerNameNormalizer.Normalize(parsedPlayer.SourceName);
        var countryCode = NormalizeCountryCode(parsedPlayer.CountryCode);

        var exists = await dbContext.SourcePlayerIdentities.AnyAsync(
            x => x.DataSourceId == dataSourceId
                && x.PlayerId == player.Id
                && x.NormalizedName == normalizedName
                && x.CountryCode == countryCode,
            cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.SourcePlayerIdentities.Add(new SourcePlayerIdentity
        {
            DataSourceId = dataSourceId,
            ExternalPlayerId = parsedPlayer.ExternalPlayerId,
            SourceName = parsedPlayer.SourceName.Trim(),
            NormalizedName = normalizedName,
            CountryCode = countryCode,
            Player = player
        });
    }

    private static string? NormalizeCountryCode(string? countryCode) =>
        string.IsNullOrWhiteSpace(countryCode)
            ? null
            : countryCode.Trim().ToUpperInvariant();
}
