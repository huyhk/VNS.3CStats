using VNS.ThreeCStats.Infrastructure.Normalization;

namespace VNS.ThreeCStats.Infrastructure.Sources.Ceb;

public sealed record CebCrawlEventResult(
    DiscoveredCebEvent Event,
    bool Imported,
    bool Skipped,
    int CreatedPlayers,
    int CreatedMatches,
    int ExistingMatches,
    int UnresolvedMatches,
    string? Message,
    string? Error);

public sealed record CebCrawlResult(
    int DiscoveredEvents,
    IReadOnlyList<CebCrawlEventResult> Events);

public sealed class CebCrawler(
    CebEventDiscoveryClient discoveryClient,
    CebCrawlPolicy crawlPolicy,
    CebIngestionService ingestionService,
    CebCanonicalImporter canonicalImporter)
{
    public async Task<CebCrawlResult> CrawlAsync(CancellationToken cancellationToken = default)
    {
        var discovered = await discoveryClient.DiscoverAsync(cancellationToken);
        var results = new List<CebCrawlEventResult>(discovered.Count);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var discoveredEvent in discovered.OrderBy(x => x.StartDate).ThenBy(x => x.ExternalId, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var decision = await crawlPolicy.DecideAsync(discoveredEvent, today, cancellationToken);
                if (!decision.ShouldCrawl)
                {
                    results.Add(new CebCrawlEventResult(
                        discoveredEvent,
                        false,
                        true,
                        0,
                        0,
                        0,
                        0,
                        decision.Reason,
                        null));
                    continue;
                }

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
                    false,
                    createdPlayers,
                    canonical.CreatedMatches,
                    canonical.ExistingMatches,
                    canonical.UnresolvedMatches,
                    decision.Reason,
                    null));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                results.Add(new CebCrawlEventResult(
                    discoveredEvent,
                    false,
                    false,
                    0,
                    0,
                    0,
                    0,
                    null,
                    ex.Message));
            }
        }

        return new CebCrawlResult(discovered.Count, results);
    }
}
