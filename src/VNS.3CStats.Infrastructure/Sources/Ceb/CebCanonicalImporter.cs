using Microsoft.EntityFrameworkCore;
using VNS.ThreeCStats.Domain.Competitions;
using VNS.ThreeCStats.Domain.Players;
using VNS.ThreeCStats.Domain.Sources;
using VNS.ThreeCStats.Infrastructure.Normalization;
using VNS.ThreeCStats.Infrastructure.Persistence;

namespace VNS.ThreeCStats.Infrastructure.Sources.Ceb;

public sealed record CebCanonicalImportResult(
    long TournamentId,
    int CreatedMatches,
    int ExistingMatches,
    int UnresolvedMatches);

public sealed class CebCanonicalImporter(AppDbContext dbContext)
{
    public async Task<CebCanonicalImportResult> ImportKnownU25EventAsync(
        CebIngestionResult ingestion,
        CancellationToken cancellationToken = default)
    {
        var tournament = await GetOrCreateKnownU25TournamentAsync(cancellationToken);
        var playerMap = await BuildPlayerMapAsync(
            ingestion.SourceDocument.DataSourceId,
            ingestion.PlayerResolutions,
            cancellationToken);

        var created = 0;
        var existing = 0;
        var unresolved = 0;

        foreach (var parsedMatch in ingestion.Matches)
        {
            var player1 = ResolveMatchPlayer(playerMap, parsedMatch.Player1.SourceName);
            var player2 = ResolveMatchPlayer(playerMap, parsedMatch.Player2.SourceName);
            if (player1 is null || player2 is null)
            {
                unresolved++;
                continue;
            }

            Match? match = null;
            if (parsedMatch.MatchNumber.HasValue)
            {
                match = await dbContext.Matches.SingleOrDefaultAsync(
                    x => x.TournamentId == tournament.Id
                        && x.MatchNumber == parsedMatch.MatchNumber,
                    cancellationToken);
            }

            if (match is null)
            {
                match = new Match
                {
                    TournamentId = tournament.Id,
                    PlayedAt = parsedMatch.PlayedAt,
                    Stage = parsedMatch.Stage,
                    Group = parsedMatch.Group,
                    MatchNumber = parsedMatch.MatchNumber,
                    TableNumber = parsedMatch.TableNumber,
                    Player1Id = player1.Id,
                    Player2Id = player2.Id,
                    Player1Score = parsedMatch.Player1Score,
                    Player2Score = parsedMatch.Player2Score,
                    Player1MatchPoints = parsedMatch.Player1MatchPoints,
                    Player2MatchPoints = parsedMatch.Player2MatchPoints,
                    Player1Innings = parsedMatch.Player1Innings,
                    Player2Innings = parsedMatch.Player2Innings,
                    Player1Average = parsedMatch.Player1Average,
                    Player2Average = parsedMatch.Player2Average,
                    Player1HighRun = parsedMatch.Player1HighRun,
                    Player2HighRun = parsedMatch.Player2HighRun,
                    WinnerId = parsedMatch.Player1Score == parsedMatch.Player2Score
                        ? null
                        : parsedMatch.Player1Score > parsedMatch.Player2Score
                            ? player1.Id
                            : player2.Id
                };
                dbContext.Matches.Add(match);
                await dbContext.SaveChangesAsync(cancellationToken);
                created++;
            }
            else
            {
                existing++;
            }

            var hasSource = await dbContext.MatchSources.AnyAsync(
                x => x.MatchId == match.Id
                    && x.SourceDocumentId == ingestion.SourceDocument.Id,
                cancellationToken);

            if (!hasSource)
            {
                dbContext.MatchSources.Add(new MatchSource
                {
                    MatchId = match.Id,
                    SourceDocumentId = ingestion.SourceDocument.Id,
                    ExternalMatchId = parsedMatch.MatchNumber?.ToString(),
                    VerificationStatus = VerificationStatus.Official
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new CebCanonicalImportResult(tournament.Id, created, existing, unresolved);
    }

    private async Task<Tournament> GetOrCreateKnownU25TournamentAsync(CancellationToken cancellationToken)
    {
        var organization = await dbContext.Organizations
            .SingleOrDefaultAsync(x => x.Code == "CEB", cancellationToken);
        if (organization is null)
        {
            organization = new Organization
            {
                Code = "CEB",
                Name = "Confédération Européenne de Billard"
            };
            dbContext.Organizations.Add(organization);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        const string competitionName = "European Championship 3-Cushion U25";
        var competition = await dbContext.Competitions
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organization.Id && x.Name == competitionName,
                cancellationToken);
        if (competition is null)
        {
            competition = new Competition
            {
                OrganizationId = organization.Id,
                Name = competitionName,
                Type = CompetitionType.ContinentalChampionship
            };
            dbContext.Competitions.Add(competition);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var tournament = await dbContext.Tournaments.SingleOrDefaultAsync(
            x => x.CompetitionId == competition.Id
                && x.SourceExternalId == CebKnownEvents.EuropeanChampionship3CushionU25_2025ExternalId,
            cancellationToken);
        if (tournament is not null)
        {
            return tournament;
        }

        tournament = new Tournament
        {
            CompetitionId = competition.Id,
            Name = "European Championship 3-Cushion U25 2025",
            StartDate = new DateOnly(2025, 3, 28),
            EndDate = new DateOnly(2025, 3, 30),
            CountryCode = "DNK",
            City = "Aarhus",
            SourceExternalId = CebKnownEvents.EuropeanChampionship3CushionU25_2025ExternalId
        };
        dbContext.Tournaments.Add(tournament);
        await dbContext.SaveChangesAsync(cancellationToken);
        return tournament;
    }

    private async Task<Dictionary<string, List<Player>>> BuildPlayerMapAsync(
        long dataSourceId,
        IReadOnlyList<CebPlayerResolution> playerResolutions,
        CancellationToken cancellationToken)
    {
        var resolvedIds = playerResolutions
            .Where(x => x.Resolution.Player is not null)
            .Select(x => x.Resolution.Player!.Id)
            .Distinct()
            .ToArray();

        var identities = await dbContext.SourcePlayerIdentities
            .AsNoTracking()
            .Include(x => x.Player)
            .Where(x => x.DataSourceId == dataSourceId && resolvedIds.Contains(x.PlayerId))
            .ToListAsync(cancellationToken);

        return identities
            .GroupBy(x => x.NormalizedName)
            .ToDictionary(
                x => x.Key,
                x => x.Select(y => y.Player).GroupBy(y => y.Id).Select(y => y.First()).ToList());
    }

    private static Player? ResolveMatchPlayer(
        IReadOnlyDictionary<string, List<Player>> playerMap,
        string sourceName)
    {
        var normalizedName = PlayerNameNormalizer.Normalize(sourceName);
        return playerMap.TryGetValue(normalizedName, out var candidates) && candidates.Count == 1
            ? candidates[0]
            : null;
    }
}
