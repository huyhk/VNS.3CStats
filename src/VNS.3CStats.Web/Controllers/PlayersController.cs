using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VNS.ThreeCStats.Infrastructure.Persistence;

namespace VNS.ThreeCStats.Web.Controllers;

[Route("players")]
public sealed class PlayersController(AppDbContext dbContext) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var players = await dbContext.Players
            .AsNoTracking()
            .OrderBy(x => x.CanonicalName)
            .Select(x => new PlayerListItemViewModel(
                x.Id,
                x.CanonicalName,
                x.CountryCode,
                dbContext.Matches.Count(m => m.Player1Id == x.Id || m.Player2Id == x.Id)))
            .ToListAsync(cancellationToken);

        return View(players);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Detail(long id, CancellationToken cancellationToken)
    {
        var player = await dbContext.Players
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (player is null)
        {
            return NotFound();
        }

        var matches = await dbContext.Matches
            .AsNoTracking()
            .Where(x => x.Player1Id == id || x.Player2Id == id)
            .Include(x => x.Player1)
            .Include(x => x.Player2)
            .Include(x => x.Tournament)
            .OrderByDescending(x => x.PlayedAt)
            .ThenByDescending(x => x.MatchNumber)
            .ToListAsync(cancellationToken);

        return View(new PlayerDetailViewModel(
            player.Id,
            player.CanonicalName,
            player.CountryCode,
            matches.Select(x => new PlayerMatchViewModel(
                x.Id,
                x.TournamentId,
                x.Tournament.Name,
                x.PlayedAt,
                x.Stage,
                x.Group,
                x.Player1Id,
                x.Player1.CanonicalName,
                x.Player1Score,
                x.Player2Id,
                x.Player2.CanonicalName,
                x.Player2Score,
                x.WinnerId)).ToArray()));
    }
}

public sealed record PlayerListItemViewModel(
    long Id,
    string Name,
    string? CountryCode,
    int MatchCount);

public sealed record PlayerDetailViewModel(
    long Id,
    string Name,
    string? CountryCode,
    IReadOnlyList<PlayerMatchViewModel> Matches);

public sealed record PlayerMatchViewModel(
    long Id,
    long TournamentId,
    string TournamentName,
    DateTimeOffset? PlayedAt,
    string? Stage,
    string? Group,
    long Player1Id,
    string Player1Name,
    int Player1Score,
    long Player2Id,
    string Player2Name,
    int Player2Score,
    long? WinnerId);
