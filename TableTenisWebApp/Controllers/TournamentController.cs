using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using TableTenisWebApp.Data;
using TableTenisWebApp.Models;
using TableTenisWebApp.Models.ViewModels;
using TableTenisWebApp.Services;
using static TableTenisWebApp.Models.CompetitionType;




namespace TableTenisWebApp.Controllers
{
    [Authorize(Roles = "Organizer,Admin")]
    public class TournamentsController : Controller
    {
        private readonly AppIdentityDbContext _ctx;
        private readonly KnockoutService _knockout;
        public TournamentsController(AppIdentityDbContext ctx, KnockoutService knockout)
        {
            _ctx = ctx;
            _knockout = knockout;
        }
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
                .Where(m => m.Score1 + m.Score2 > 0 && m.Player1Id != m.Player2Id)  // pomiń BYE  
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
                        Points: won);
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

            if (t.Matches.Any())
            {
                TempData["Err"] = "Terminarz już istnieje.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var players = t.Players.Select(tp => tp.Player).OrderBy(p => Guid.NewGuid()).ToList();

            if (t.Type == CompetitionType.Knockout)
            {
                if (await _knockout.GenerateFirstRoundAsync(t))
                    TempData["Msg"] = "Pierwsza runda została wygenerowana.";
                else
                    TempData["Err"] = "Nie udało się wygenerować rundy.";
            }
            else if (t.Type == CompetitionType.League)
            {
                // Dodaj BYE jeśli liczba nieparzysta
                if (players.Count % 2 == 1)
                    players.Add(null); // null = wolny los

                int n = players.Count;
                int rounds = n - 1;
                int half = n / 2;

                for (int r = 1; r <= rounds; r++)
                {
                    for (int i = 0; i < half; i++)
                    {
                        var p1 = players[i];
                        var p2 = players[n - 1 - i];

                        if (p1 != null && p2 != null)
                        {
                            t.Matches.Add(new Match
                            {
                                Tournament = t,
                                Player1Id = p1.Id,
                                Player2Id = p2.Id,
                                RoundNumber = r,
                                DatePlayed = DateTime.Today.AddDays(r - 1)
                            });
                        }
                    }

                    // Round-robin obrót
                    var temp = players[n - 1];
                    for (int i = n - 1; i > 1; i--)
                        players[i] = players[i - 1];
                    players[1] = temp;
                }

                TempData["Msg"] = "Terminarz ligowy został wygenerowany.";
            }

            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }
        //GENERATE NEXT ROUND//
        [HttpPost]
        public async Task<IActionResult> GenerateNextRound(int id)
        {
            var t = await _ctx.Tournaments
                .Include(x => x.Matches)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (t is null) return NotFound();

            int currentRound = t.Matches.Max(m => m.RoundNumber);
            bool allFinished = t.Matches
                .Where(m => m.RoundNumber == currentRound)
                .All(m => m.IsApproved);

            if (!allFinished)
            {
                TempData["Err"] = "Nie wszystkie mecze tej rundy zostały zatwierdzone.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (await _knockout.GenerateNextRoundAsync(t))
                TempData["Msg"] = "Wygenerowano kolejną rundę.";
            else
                TempData["Err"] = "Turniej zakończony.";

            return RedirectToAction(nameof(Details), new { id });
        }
        [AllowAnonymous]
        public async Task<IActionResult> Bracket(int id)
        {
            var t = await _ctx.Tournaments
                .Include(x => x.Players).ThenInclude(tp => tp.Player)
                .Include(x => x.Matches)
                    .ThenInclude(m => m.Player1)
                .Include(x => x.Matches)
                    .ThenInclude(m => m.Player2)
                .FirstOrDefaultAsync(x => x.Id == id);

            return t == null ? NotFound() : View(t);
        }


    }
}
