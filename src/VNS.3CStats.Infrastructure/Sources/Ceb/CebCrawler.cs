using VNS.ThreeCStats.Infrastructure.Normalization;

namespace VNS.ThreeCStats.Infrastructure.Sources.Ceb;

public sealed record CebCrawlEventResult(
    DiscoveredCebEvent Event,
    bool Imported,
    int CreatedPlayers,
    int CreatedMatches,
    int ExistingMatches,
    int UnresolvedMatches,
    string? Error);

public sealed record CebCrawlResult(
    int DiscoveredEvents,
    IReadOnlyList<CebCrawlEventResult> Events);

public sealed class CebCrawler(
    CebEventDiscoveryClient discoveryClient,
    CebIngestionService ingestionService,
    CebCanonicalImporter canonicalImporter)
{
    public async Task<CebCrawlResult> CrawlAsync(CancellationToken cancellationToken = default)
    {
        var discovered = await discoveryClient.DiscoverAsync(cancellationToken);
        var results = new List<CebCrawlEventResult>(discovered.Count);

        foreach (var discoveredEvent in discovered.OrderBy(x => x.StartDate).ThenBy(x => x.ExternalId, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var ingestion = await ingestionService.FetchAndParseAsync(
                    discoveredEvent.Url,
                    discoveredEvent.ExternalId,
                    cancellationToken);

                var unresolvedBeforeImport = ingestion.PlayerResolutions.Count(
                    x => x.Resolution.Kind == PlayerIdentityResolutionKind.Unresolved);
                var importedPlayers = unresolvedBeforeImport > 0
                    ? await ingestionService.FetchAndImportPlayersAsync(
                        discoveredEvent.Url,
                        discoveredEvent.ExternalId,
                        cancellationToken)
                    : ingestion;

                var canonical = await canonicalImporter.ImportAsync(importedPlayers, cancellationToken);
                var createdPlayers = importedPlayers.PlayerResolutions.Count(
                    x => x.Resolution.Kind == PlayerIdentityResolutionKind.Created);

                results.Add(new CebCrawlEventResult(
                    discoveredEvent,
                    true,
                    createdPlayers,
                    canonical.CreatedMatches,
                    canonical.ExistingMatches,
                    canonical.UnresolvedMatches,
                    null));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                results.Add(new CebCrawlEventResult(
                    discoveredEvent,
                    false,
                    0,
                    0,
                    0,
                    0,
                    ex.Message));
            }
        }

        return new CebCrawlResult(discovered.Count, results);
    }
}
