using Microsoft.EntityFrameworkCore;
using VNS.ThreeCStats.Infrastructure.Persistence;

namespace VNS.ThreeCStats.Infrastructure.Sources.Ceb;

public enum CebCrawlDecisionKind
{
    Crawl = 0,
    SkipCompleted = 1
}

public sealed record CebCrawlDecision(CebCrawlDecisionKind Kind, string Reason)
{
    public bool ShouldCrawl => Kind == CebCrawlDecisionKind.Crawl;
}

public sealed class CebCrawlPolicy(AppDbContext dbContext)
{
    private const int CompletedEventGraceDays = 2;

    public async Task<CebCrawlDecision> DecideAsync(
        DiscoveredCebEvent discoveredEvent,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var existingTournament = await dbContext.Tournaments
            .AsNoTracking()
            .Where(x => x.SourceExternalId == discoveredEvent.ExternalId)
            .Select(x => new { x.Id, x.EndDate })
            .FirstOrDefaultAsync(cancellationToken);

        if (existingTournament is null)
        {
            return new CebCrawlDecision(CebCrawlDecisionKind.Crawl, "Event has not been imported yet.");
        }

        var effectiveEndDate = existingTournament.EndDate ?? discoveredEvent.EndDate ?? discoveredEvent.StartDate;
        if (!effectiveEndDate.HasValue)
        {
            return new CebCrawlDecision(CebCrawlDecisionKind.Crawl, "Existing event has no reliable end date.");
        }

        if (effectiveEndDate.Value.AddDays(CompletedEventGraceDays) >= today)
        {
            return new CebCrawlDecision(CebCrawlDecisionKind.Crawl, "Event is active or recently completed and may still change.");
        }

        var hasMatches = await dbContext.Matches
            .AsNoTracking()
            .AnyAsync(x => x.TournamentId == existingTournament.Id, cancellationToken);

        return hasMatches
            ? new CebCrawlDecision(CebCrawlDecisionKind.SkipCompleted, "Completed event is already imported.")
            : new CebCrawlDecision(CebCrawlDecisionKind.Crawl, "Completed event exists but has no imported matches.");
    }
}
