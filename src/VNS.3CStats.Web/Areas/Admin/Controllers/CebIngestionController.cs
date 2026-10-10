using Microsoft.AspNetCore.Mvc;
using VNS.ThreeCStats.Infrastructure.Sources.Ceb;

namespace VNS.ThreeCStats.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/ingestion/ceb")]
public sealed class CebIngestionController(CebIngestionService ingestionService) : Controller
{
    [HttpPost("verify-known-event")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyKnownEvent(CancellationToken cancellationToken)
    {
        var result = await ingestionService.FetchAndParseAsync(
            new Uri(CebKnownEvents.EuropeanChampionship3CushionU25_2025),
            CebKnownEvents.EuropeanChampionship3CushionU25_2025ExternalId,
            cancellationToken);

        return Ok(new
        {
            sourceDocumentId = result.SourceDocument.Id,
            result.SourceDocument.ExternalId,
            result.SourceDocument.Url,
            result.SourceDocument.RetrievedAt,
            result.SourceDocument.ContentHash,
            result.SourceDocument.ParserVersion,
            matchCount = result.Matches.Count,
            firstMatch = result.Matches.FirstOrDefault() is { } first
                ? new
                {
                    first.MatchNumber,
                    first.TableNumber,
                    first.PlayedAt,
                    first.Stage,
                    first.Group,
                    player1 = first.Player1.SourceName,
                    player2 = first.Player2.SourceName,
                    first.Player1Score,
                    first.Player2Score
                }
                : null
        });
    }
}
