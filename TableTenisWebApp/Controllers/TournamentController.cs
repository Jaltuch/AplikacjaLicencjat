using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using TableTenisWebApp.Data;
using TableTenisWebApp.Models;
using TableTenisWebApp.Models.ViewModels;
using static TableTenisWebApp.Models.CompetitionType;




namespace TableTenisWebApp.Controllers
{
    [Authorize(Roles = "Organizer,Admin")]
    public class TournamentsController : Controller
    {
        private readonly AppIdentityDbContext _ctx;
        public TournamentsController(AppIdentityDbContext ctx) => _ctx = ctx;

        /* -----------------------------------------------------------
         *  LISTA + SZCZEGÓŁY (publiczne)
         * ----------------------------------------------------------*/

        [AllowAnonymous]
        public async Task<IActionResult> Index()
            => View(await _ctx.Tournaments.AsNoTracking().ToListAsync());

        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return NotFound();

            var t = await _ctx.Tournaments
                .Include(x => x.Players).ThenInclude(tp => tp.Player)
                .Include(x => x.Matches).ThenInclude(m => m.Player1).ThenInclude(p => p.ApplicationUser)
                .Include(x => x.Matches).ThenInclude(m => m.Player2).ThenInclude(p => p.ApplicationUser)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (t is null) return NotFound();

            /* ---------- RANKING ---------- */
            var standings = t.Matches
                .Where(m => m.Score1 + m.Score2 > 0)        // tylko rozegrane
                .SelectMany(m => new[]
                {
                    new { Player = m.Player1, Scored = m.Score1, Lost = m.Score2 },
                    new { Player = m.Player2, Scored = m.Score2, Lost = m.Score1 }
                })
                .GroupBy(x => x.Player!.Id)
                .Select(g =>
                {
                    var won = g.Count(r => r.Scored > r.Lost);
                    var lost = g.Count(r => r.Scored < r.Lost);

                    return new TournamentStandingRow(
                        PlayerId: g.Key,
                        Name: g.First().Player!.Name,
                        Played: won + lost,
                        Won: won,
                        Lost: lost,
                        SetsPlus: g.Sum(r => r.Scored),
                        SetsMinus: g.Sum(r => r.Lost),
                        Points: won * 2);
                })
                .OrderByDescending(r => r.Points)
                .ThenByDescending(r => r.SetsPlus - r.SetsMinus)
                .ThenBy(r => r.Name)
                .ToList();

            ViewBag.Standings = standings;
            return View(t);
        }

        /* -----------------------------------------------------------
         *  CREATE
         * ----------------------------------------------------------*/

        public IActionResult Create()
        {
            ViewBag.AllPlayers = _ctx.Players.ToList();
            ViewBag.Types = new SelectList(
                Enum.GetValues(typeof(CompetitionType))
                    .Cast<CompetitionType>()
                    .Select(t => new { Id = (int)t, Name = t.ToString() }),
                "Id", "Name");

            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Tournament t, int[] selectedPlayers)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.AllPlayers = _ctx.Players.ToList();
                return View(t);
            }

            foreach (var pid in selectedPlayers)
                t.Players.Add(new TournamentPlayer { PlayerId = pid });

            _ctx.Tournaments.Add(t);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        /* -----------------------------------------------------------
         *  EDIT
         * ----------------------------------------------------------*/

        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null) return NotFound();

            var t = await _ctx.Tournaments
                              .Include(x => x.Players)
                              .FirstOrDefaultAsync(x => x.Id == id);
            if (t is null) return NotFound();

            ViewBag.Types = new SelectList(
                Enum.GetValues(typeof(CompetitionType))
                    .Cast<CompetitionType>()
                    .Select(t => new { Id = (int)t, Name = t.ToString() }),
                "Id", "Name", (int)t.Type);   // trzecie = domyślnie zaznaczone

            ViewBag.AllPlayers = _ctx.Players.ToList();
            ViewBag.SelectedIds = t.Players.Select(p => p.PlayerId).ToArray();
            return View(t);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Tournament t, int[] selectedPlayers)
        {
            if (id != t.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.AllPlayers = _ctx.Players.ToList();
                ViewBag.SelectedIds = selectedPlayers;
                return View(t);
            }

            var dbT = await _ctx.Tournaments
                                .Include(x => x.Players)
                                .FirstAsync(x => x.Id == id);

            dbT.Name = t.Name;
            dbT.Start = t.Start;
            dbT.End = t.End;

            dbT.Players.Clear();
            foreach (var pid in selectedPlayers)
                dbT.Players.Add(new TournamentPlayer { PlayerId = pid });

            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        /* -----------------------------------------------------------
         *  DELETE
         * ----------------------------------------------------------*/

        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null) return NotFound();
            var t = await _ctx.Tournaments.FindAsync(id);
            return t is null ? NotFound() : View(t);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var t = await _ctx.Tournaments.FindAsync(id);
            if (t is not null) _ctx.Tournaments.Remove(t);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        // ---------- GENERATE FIXTURES ----------
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(int id)
        {
            var t = await _ctx.Tournaments
                .Include(x => x.Players).ThenInclude(tp => tp.Player)
                .Include(x => x.Matches)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (t is null) return NotFound();
            if (t.Matches.Any())              // nie pozwalamy generować 2×
            {
                TempData["Err"] = "Terminarz już istnieje.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // --- wspólne dane ---
            var rnd = new Random();
            var players = t.Players.Select(tp => tp.Player).OrderBy(_ => rnd.Next()).ToList();

            switch (t.Type)
            {
                case Knockout:
                    // 1-round knockout  ▸ paruj sąsiadów
                    if (players.Count % 2 == 1)
                    {
                        TempData["Err"] = "Na drabinkę pucharową potrzebna parzysta liczba graczy.";
                        return RedirectToAction(nameof(Details), new { id });
                    }
                    for (int i = 0; i < players.Count; i += 2)
                        t.Matches.Add(new Match
                        {
                            Player1Id = players[i].Id,
                            Player2Id = players[i + 1].Id,
                            DatePlayed = DateTime.Today,
                            Tournament = t
                        });
                    break;

                case League:
                    // każdy-z-każdym raz
                    for (int i = 0; i < players.Count; ++i)
                        for (int j = i + 1; j < players.Count; ++j)
                            t.Matches.Add(new Match
                            {
                                Player1Id = players[i].Id,
                                Player2Id = players[j].Id,
                                DatePlayed = DateTime.Today,
                                Tournament = t
                            });
                    break;
            }

            await _ctx.SaveChangesAsync();
            TempData["Msg"] = "Terminarz wygenerowany.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
