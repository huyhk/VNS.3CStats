using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VNS.ThreeCStats.Infrastructure.Persistence;

namespace VNS.ThreeCStats.Web.Controllers;

[Route("h2h")]
public sealed class HeadToHeadController(AppDbContext dbContext) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(long? player1, long? player2, CancellationToken cancellationToken)
    {
        var players = await dbContext.Players
            .AsNoTracking()
            .OrderBy(x => x.CanonicalName)
            .Select(x => new HeadToHeadPlayerOptionViewModel(x.Id, x.CanonicalName, x.CountryCode))
            .ToListAsync(cancellationToken);

        if (!player1.HasValue || !player2.HasValue || player1 == player2)
        {
            return View(new HeadToHeadViewModel(players, player1, player2, null));
        }

        var selectedPlayers = await dbContext.Players
            .AsNoTracking()
            .Where(x => x.Id == player1.Value || x.Id == player2.Value)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (selectedPlayers.Count != 2)
        {
            return NotFound();
        }

        var matches = await dbContext.Matches
            .AsNoTracking()
            .Where(x =>
                (x.Player1Id == player1.Value && x.Player2Id == player2.Value) ||
                (x.Player1Id == player2.Value && x.Player2Id == player1.Value))
            .OrderByDescending(x => x.PlayedAt)
            .ThenByDescending(x => x.MatchNumber)
            .Select(x => new HeadToHeadMatchViewModel(
                x.Id,
                x.TournamentId,
                x.Tournament.Name,
                x.PlayedAt,
                x.Stage,
                x.Player1Id,
                x.Player1.CanonicalName,
                x.Player1Score,
                x.Player2Id,
                x.Player2.CanonicalName,
                x.Player2Score,
                x.WinnerId))
            .ToListAsync(cancellationToken);

        var player1Wins = matches.Count(x => x.WinnerId == player1.Value);
        var player2Wins = matches.Count(x => x.WinnerId == player2.Value);
        var draws = matches.Count - player1Wins - player2Wins;

        var result = new HeadToHeadResultViewModel(
            player1.Value,
            selectedPlayers[player1.Value].CanonicalName,
            player2.Value,
            selectedPlayers[player2.Value].CanonicalName,
            matches.Count,
            player1Wins,
            player2Wins,
            draws,
            matches);

        return View(new HeadToHeadViewModel(players, player1, player2, result));
    }
}

public sealed record HeadToHeadPlayerOptionViewModel(long Id, string Name, string? CountryCode);

public sealed record HeadToHeadViewModel(
    IReadOnlyList<HeadToHeadPlayerOptionViewModel> Players,
    long? Player1Id,
    long? Player2Id,
    HeadToHeadResultViewModel? Result);

public sealed record HeadToHeadResultViewModel(
    long Player1Id,
    string Player1Name,
    long Player2Id,
    string Player2Name,
    int TotalMatches,
    int Player1Wins,
    int Player2Wins,
    int Draws,
    IReadOnlyList<HeadToHeadMatchViewModel> Matches);

public sealed record HeadToHeadMatchViewModel(
    long Id,
    long TournamentId,
    string TournamentName,
    DateTime? PlayedAt,
    string? Stage,
    long Player1Id,
    string Player1Name,
    int Player1Score,
    long Player2Id,
    string Player2Name,
    int Player2Score,
    long? WinnerId);
