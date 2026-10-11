using Microsoft.EntityFrameworkCore;
using VNS.ThreeCStats.Application.Ingestion;
using VNS.ThreeCStats.Domain.Sources;
using VNS.ThreeCStats.Infrastructure.Normalization;
using VNS.ThreeCStats.Infrastructure.Persistence;

namespace VNS.ThreeCStats.Infrastructure.Sources.Ceb;

public sealed record CebPlayerResolution(
    ParsedPlayer ParsedPlayer,
    PlayerIdentityResolution Resolution);

public sealed record CebIngestionResult(
    SourceDocument SourceDocument,
    IReadOnlyList<CebPlayerResolution> PlayerResolutions,
    IReadOnlyList<ParsedMatch> Matches)
{
    public IReadOnlyList<ParsedPlayer> Players =>
        PlayerResolutions.Select(x => x.ParsedPlayer).ToArray();
}

public sealed class CebIngestionService(
    AppDbContext dbContext,
    CebSourceClient sourceClient,
    CebPlayerParser playerParser,
    CebMatchParser matchParser,
    PlayerIdentityResolver playerIdentityResolver,
    PlayerIdentityImporter playerIdentityImporter)
{
    public Task<CebIngestionResult> FetchAndParseAsync(
        Uri url,
        string? externalId = null,
        CancellationToken cancellationToken = default) =>
        FetchAsync(url, externalId, importPlayers: false, cancellationToken);

    public Task<CebIngestionResult> FetchAndImportPlayersAsync(
        Uri url,
        string? externalId = null,
        CancellationToken cancellationToken = default) =>
        FetchAsync(url, externalId, importPlayers: true, cancellationToken);

    private async Task<CebIngestionResult> FetchAsync(
        Uri url,
        string? externalId,
        bool importPlayers,
        CancellationToken cancellationToken)
    {
        var fetched = await sourceClient.FetchAsync(url, externalId, cancellationToken);
        var dataSource = await GetOrCreateDataSourceAsync(cancellationToken);

        fetched.Document.DataSource = dataSource;
        fetched.Document.DataSourceId = dataSource.Id;

        dbContext.SourceDocuments.Add(fetched.Document);
        await dbContext.SaveChangesAsync(cancellationToken);

        var players = playerParser.ParsePlayers(fetched.Content);
        var matches = matchParser.ParseMatches(fetched.Content);
        var resolutions = new List<CebPlayerResolution>(players.Count);

        foreach (var player in players)
        {
            var resolution = importPlayers
                ? await playerIdentityImporter.ImportAsync(dataSource.Id, player, cancellationToken)
                : await playerIdentityResolver.ResolveAsync(
                    dataSource.Id,
                    player.ExternalPlayerId,
                    player.SourceName,
                    player.CountryCode,
                    cancellationToken);

            resolutions.Add(new CebPlayerResolution(player, resolution));
        }

        return new CebIngestionResult(fetched.Document, resolutions, matches);
    }

    private async Task<DataSource> GetOrCreateDataSourceAsync(CancellationToken cancellationToken)
    {
        var existing = await dbContext.DataSources
            .SingleOrDefaultAsync(x => x.Code == CebSourceClient.SourceCode, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var source = new DataSource
        {
            Code = CebSourceClient.SourceCode,
            Name = "Confédération Européenne de Billard",
            BaseUrl = "https://www.eurobillard.org/",
            SourceType = "Official federation website"
        };

        dbContext.DataSources.Add(source);
        return source;
    }
}
