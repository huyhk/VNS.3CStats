using Microsoft.EntityFrameworkCore;
using VNS.ThreeCStats.Application.Ingestion;
using VNS.ThreeCStats.Domain.Sources;
using VNS.ThreeCStats.Infrastructure.Persistence;

namespace VNS.ThreeCStats.Infrastructure.Sources.Ceb;

public sealed record CebIngestionResult(SourceDocument SourceDocument, IReadOnlyList<ParsedMatch> Matches);

public sealed class CebIngestionService(
    AppDbContext dbContext,
    CebSourceClient sourceClient,
    CebMatchParser matchParser)
{
    public async Task<CebIngestionResult> FetchAndParseAsync(
        Uri url,
        string? externalId = null,
        CancellationToken cancellationToken = default)
    {
        var fetched = await sourceClient.FetchAsync(url, externalId, cancellationToken);
        var dataSource = await GetOrCreateDataSourceAsync(cancellationToken);

        fetched.Document.DataSource = dataSource;
        fetched.Document.DataSourceId = dataSource.Id;

        dbContext.SourceDocuments.Add(fetched.Document);
        await dbContext.SaveChangesAsync(cancellationToken);

        var matches = matchParser.ParseMatches(fetched.Content);
        return new CebIngestionResult(fetched.Document, matches);
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
