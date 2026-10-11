using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VNS.ThreeCStats.Infrastructure.Persistence;

namespace VNS.ThreeCStats.Web.Controllers;

[Route("tournaments")]
public sealed class TournamentsController(AppDbContext dbContext) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var tournaments = await dbContext.Tournaments
            .AsNoTracking()
            .OrderByDescending(x => x.StartDate)
            .ThenBy(x => x.Name)
            .Select(x => new TournamentListItemViewModel(
                x.Id,
                x.Name,
                x.StartDate,
                x.EndDate,
                x.City,
                x.CountryCode,
                x.Competition.Organization.Code,
                dbContext.Matches.Count(m => m.TournamentId == x.Id)))
            .ToListAsync(cancellationToken);

        return View(tournaments);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Detail(long id, CancellationToken cancellationToken)
    {
        var tournament = await dbContext.Tournaments
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.StartDate,
                x.EndDate,
                x.City,
                x.CountryCode,
                CompetitionName = x.Competition.Name,
                OrganizationCode = x.Competition.Organization.Code
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (tournament is null)
        {
            return NotFound();
        }

        var matches = await dbContext.Matches
            .AsNoTracking()
            .Where(x => x.TournamentId == id)
            .OrderBy(x => x.PlayedAt)
            .ThenBy(x => x.MatchNumber)
            .Select(x => new TournamentMatchViewModel(
                x.Id,
                x.PlayedAt,
                x.MatchNumber,
                x.TableNumber,
                x.Stage,
                x.Group,
                x.Player1Id,
                x.Player1.CanonicalName,
                x.Player1Score,
                x.Player1Innings,
                x.Player1Average,
                x.Player1HighRun,
                x.Player2Id,
                x.Player2.CanonicalName,
                x.Player2Score,
                x.Player2Innings,
                x.Player2Average,
                x.Player2HighRun,
                x.WinnerId))
            .ToListAsync(cancellationToken);

        return View(new TournamentDetailViewModel(
            tournament.Id,
            tournament.Name,
            tournament.StartDate,
            tournament.EndDate,
            tournament.City,
            tournament.CountryCode,
            tournament.CompetitionName,
            tournament.OrganizationCode,
            matches));
    }
}

public sealed record TournamentListItemViewModel(
    long Id,
    string Name,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? City,
    string? CountryCode,
    string OrganizationCode,
    int MatchCount);

public sealed record TournamentDetailViewModel(
    long Id,
    string Name,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? City,
    string? CountryCode,
    string CompetitionName,
    string OrganizationCode,
    IReadOnlyList<TournamentMatchViewModel> Matches);

public sealed record TournamentMatchViewModel(
    long Id,
    DateTime? PlayedAt,
    int? MatchNumber,
    int? TableNumber,
    string? Stage,
    string? Group,
    long Player1Id,
    string Player1Name,
    int Player1Score,
    int? Player1Innings,
    decimal? Player1Average,
    int? Player1HighRun,
    long Player2Id,
    string Player2Name,
    int Player2Score,
    int? Player2Innings,
    decimal? Player2Average,
    int? Player2HighRun,
    long? WinnerId);
