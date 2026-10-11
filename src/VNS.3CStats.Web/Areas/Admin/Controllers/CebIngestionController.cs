using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VNS.ThreeCStats.Infrastructure;
using VNS.ThreeCStats.Infrastructure.Sources.Ceb;
using VNS.ThreeCStats.Web.Areas.Admin.Models;

namespace VNS.ThreeCStats.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = DependencyInjection.SuperAdminPolicy)]
[Route("admin/ingestion/ceb")]
public sealed class CebIngestionController(CebIngestionService ingestionService, CebCanonicalImporter canonicalImporter) : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View(CreateEmptyModel());

    [HttpPost("verify-event")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyEvent(string eventUrl, string? externalId, CancellationToken cancellationToken)
    {
        if (!TryGetEventUri(eventUrl, out var uri))
        {
            ModelState.AddModelError(nameof(eventUrl), "Enter a valid CEB event URL.");
            return View("Index", CreateEmptyModel(eventUrl, externalId));
        }

        var result = await ingestionService.FetchAndParseAsync(uri, Clean(externalId), cancellationToken);
        return View("Index", CreateResultModel(result, false));
    }

    [HttpPost("import-event-players")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportEventPlayers(string eventUrl, string? externalId, CancellationToken cancellationToken)
    {
        if (!TryGetEventUri(eventUrl, out var uri)) return BadRequest("Invalid CEB event URL.");
        var result = await ingestionService.FetchAndImportPlayersAsync(uri, Clean(externalId), cancellationToken);
        return View("Index", CreateResultModel(result, true));
    }

    [HttpPost("import-event")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportEvent(string eventUrl, string? externalId, CancellationToken cancellationToken)
    {
        if (!TryGetEventUri(eventUrl, out var uri)) return BadRequest("Invalid CEB event URL.");
        var ingestion = await ingestionService.FetchAndParseAsync(uri, Clean(externalId), cancellationToken);
        var canonicalImport = await canonicalImporter.ImportAsync(ingestion, cancellationToken);
        return View("Index", CreateResultModel(ingestion, false, canonicalImport));
    }

    private static bool TryGetEventUri(string? value, out Uri uri)
    {
        var ok = Uri.TryCreate(value, UriKind.Absolute, out var candidate)
            && candidate.Scheme == Uri.UriSchemeHttps
            && candidate.Host.Equals("www.eurobillard.org", StringComparison.OrdinalIgnoreCase)
            && candidate.AbsolutePath.StartsWith("/events/", StringComparison.OrdinalIgnoreCase);
        uri = ok ? candidate! : null!;
        return ok;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static CebIngestionViewModel CreateResultModel(CebIngestionResult result, bool playersImported, CebCanonicalImportResult? canonicalImport = null) => new()
    {
        EventUrl = result.SourceDocument.Url,
        ExternalId = result.SourceDocument.ExternalId ?? string.Empty,
        Event = result.Event,
        SourceDocumentId = result.SourceDocument.Id,
        RetrievedAt = result.SourceDocument.RetrievedAt,
        ContentHash = result.SourceDocument.ContentHash,
        ParserVersion = result.SourceDocument.ParserVersion,
        PlayerResolutions = result.PlayerResolutions,
        Matches = result.Matches,
        PlayersImported = playersImported,
        CanonicalImport = canonicalImport
    };

    private static CebIngestionViewModel CreateEmptyModel(string? eventUrl = null, string? externalId = null) => new()
    {
        EventUrl = string.IsNullOrWhiteSpace(eventUrl) ? CebKnownEvents.EuropeanChampionship3CushionU25_2025 : eventUrl,
        ExternalId = externalId ?? CebKnownEvents.EuropeanChampionship3CushionU25_2025ExternalId
    };
}
