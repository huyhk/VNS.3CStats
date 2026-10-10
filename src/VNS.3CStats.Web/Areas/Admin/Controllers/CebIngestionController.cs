using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VNS.ThreeCStats.Infrastructure;
using VNS.ThreeCStats.Infrastructure.Sources.Ceb;
using VNS.ThreeCStats.Web.Areas.Admin.Models;

namespace VNS.ThreeCStats.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = DependencyInjection.SuperAdminPolicy)]
[Route("admin/ingestion/ceb")]
public sealed class CebIngestionController(CebIngestionService ingestionService) : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return View(CreateEmptyModel());
    }

    [HttpPost("verify-known-event")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyKnownEvent(CancellationToken cancellationToken)
    {
        var result = await ingestionService.FetchAndParseAsync(
            new Uri(CebKnownEvents.EuropeanChampionship3CushionU25_2025),
            CebKnownEvents.EuropeanChampionship3CushionU25_2025ExternalId,
            cancellationToken);

        return View("Index", new CebIngestionViewModel
        {
            EventUrl = result.SourceDocument.Url,
            ExternalId = result.SourceDocument.ExternalId ?? string.Empty,
            SourceDocumentId = result.SourceDocument.Id,
            RetrievedAt = result.SourceDocument.RetrievedAt,
            ContentHash = result.SourceDocument.ContentHash,
            ParserVersion = result.SourceDocument.ParserVersion,
            Matches = result.Matches
        });
    }

    private static CebIngestionViewModel CreateEmptyModel() => new()
    {
        EventUrl = CebKnownEvents.EuropeanChampionship3CushionU25_2025,
        ExternalId = CebKnownEvents.EuropeanChampionship3CushionU25_2025ExternalId
    };
}
