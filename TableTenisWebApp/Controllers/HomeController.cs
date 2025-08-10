using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TableTenisWebApp.Data;
using TableTenisWebApp.Models;

namespace TableTenisWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppIdentityDbContext _context;

        public HomeController(AppIdentityDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Liczniki g³ówne
            var playersCount = await _context.Players.CountAsync();
            var matchesCount = await _context.Matches.CountAsync();
            var activeTournamentsCount = await _context.Tournaments.CountAsync(t => t.End == null); // wszystko bez daty koñca

            // Najlepszy gracz wg liczby wygranych meczów
            var allMatches = await _context.Matches.ToListAsync();
            var bestPlayer = _context.Players
                .ToList()
                .Select(p =>
                {
                    int wins =
                        allMatches.Count(m => (m.Player1Id == p.Id && m.Score1 > m.Score2) ||
                                              (m.Player2Id == p.Id && m.Score2 > m.Score1));
                    return new { Player = p, Wins = wins };
                })
                .OrderByDescending(x => x.Wins)
                .FirstOrDefault();

            // Nadchodz¹ce turnieje (max 5)
            var upcomingTournaments = await _context.Tournaments
                .Include(t => t.Players)
                .Where(t => !t.HasStarted && t.End == null &&
                    (!t.MaxPlayers.HasValue || t.Players.Count < t.MaxPlayers))
                .OrderBy(t => t.Start)
                .Take(5)
                .AsNoTracking()
                .ToListAsync();

            // Ostatnie mecze (max 5)
            var recentMatches = await _context.Matches
                .Include(m => m.Player1)
                .Include(m => m.Player2)
                .Where(m => m.DatePlayed != null)
                .OrderByDescending(m => m.DatePlayed)
                .Take(5)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.PlayersCount = playersCount;
            ViewBag.MatchesCount = matchesCount;
            ViewBag.TournamentsCount = activeTournamentsCount;

            ViewBag.BestPlayer = bestPlayer?.Player?.Name ?? "Brak";
            ViewBag.BestPlayerWins = bestPlayer?.Wins ?? 0;

            ViewBag.UpcomingTournaments = upcomingTournaments;
            ViewBag.RecentMatches = recentMatches;

            return View();
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
