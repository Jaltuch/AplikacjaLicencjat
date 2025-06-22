using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TableTenisWebApp.Models;
using Microsoft.EntityFrameworkCore;
using TableTenisWebApp.Data;

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
            var playersCount = await _context.Players.CountAsync();
            var matchesCount = await _context.Matches.CountAsync();

            // Najlepszy zawodnik wg liczby wygranych meczów
            var matches = await _context.Matches.ToListAsync();

            var bestPlayer = _context.Players
                .ToList()
                .Select(p =>
                {
                    int wins =
                        matches.Count(m => (m.Player1Id == p.Id && m.Score1 > m.Score2) ||
                                          (m.Player2Id == p.Id && m.Score2 > m.Score1));
                    return new { Player = p, Wins = wins };
                })
                .OrderByDescending(x => x.Wins)
                .FirstOrDefault();

            ViewBag.PlayersCount = playersCount;
            ViewBag.MatchesCount = matchesCount;
            ViewBag.BestPlayer = bestPlayer?.Player?.Name ?? "Brak";
            ViewBag.BestPlayerWins = bestPlayer?.Wins ?? 0;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
