using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using TableTenisWebApp.Data;
using TableTenisWebApp.Models;
using TableTenisWebApp.Models.ViewModels;

namespace TableTenisWebApp.Controllers
{
    [Authorize]
    public class MatchesController : Controller
    {
        /* ---------- POMOCNICZE ---------- */
        private static (int p1, int p2) SetWins(string setScores)
        {
            if (string.IsNullOrWhiteSpace(setScores)) return (0, 0);

            int w1 = 0, w2 = 0;
            foreach (var s in setScores.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = s.Split(':');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int a) &&
                    int.TryParse(parts[1], out int b))
                {
                    if (a > b) w1++; else if (b > a) w2++;
                }
            }
            return (w1, w2);
        }

        /* ---------- KONSTRUKTOR ---------- */
        private readonly AppIdentityDbContext _ctx;
        private readonly UserManager<ApplicationUser> _userManager;
        public MatchesController(AppIdentityDbContext ctx, UserManager<ApplicationUser> userManager)
        {
            _ctx = ctx;
            _userManager = userManager;
        }
        /* ---------- LISTA ---------- */
        public async Task<IActionResult> Index()
            => View(await _ctx.Matches
                               .Include(m => m.Player1)
                               .Include(m => m.Player2)
                               .Include(m => m.Tournament)
                               .AsNoTracking()
                               .ToListAsync());

        /* ---------- SZCZEGÓŁY ---------- */
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return NotFound();

            var m = await _ctx.Matches
                              .Include(x => x.Player1)
                              .Include(x => x.Player2)
                              .Include(x => x.Tournament)
                              .FirstOrDefaultAsync(x => x.Id == id);

            return m is null ? NotFound() : View(m);
        }

        /* ==============================================================
           CREATE
        =================================================================*/

        // GET
        public IActionResult Create(int numberOfSets = 5)
        {
            var vm = new MatchViewModel
            {
                NumberOfSets = numberOfSets,
                Sets = Enumerable.Range(0, numberOfSets)
                                         .Select(_ => new SetInput())
                                         .ToList(),
                DatePlayed = DateTime.Now
            };

            ViewBag.Players = new SelectList(_ctx.Players, "Id", "Name");
            ViewBag.Tournaments = new SelectList(_ctx.Tournaments, "Id", "Name");
            return View(vm);
        }

        // POST
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MatchViewModel vm)
        {
            /* ---- WALIDACJA PODSTAWOWA ---- */
            if (vm.Player1Id == vm.Player2Id)
                ModelState.AddModelError("", "Zawodnik nie może grać przeciwko samemu sobie.");

            if (vm.Sets == null || !vm.Sets.Any(s => s.Score1.HasValue && s.Score2.HasValue))
                ModelState.AddModelError("", "Wpisz wynik przynajmniej jednego seta.");

            var wins1 = vm.Sets.Count(s => s.Score1 > s.Score2);
            var wins2 = vm.Sets.Count(s => s.Score2 > s.Score1);
            var setsToWin = vm.NumberOfSets / 2 + 1;

            if ((wins1 == setsToWin && wins2 > 0) || (wins2 == setsToWin && wins1 > 0))
            {
                // OK – jeden gracz wygrał dokładnie tyle setów, ile trzeba
            }
            else
            {
                ModelState.AddModelError("", $"Zwycięzca musi wygrać dokładnie {setsToWin} sety, przeciwnik nie może mieć tylu samo lub więcej.");
            }

            /* ---- ZAPIS ---- */
            if (ModelState.IsValid)
            {
                var setString = string.Join(";", vm.Sets.Select(s => $"{s.Score1}:{s.Score2}"));
                var (s1, s2) = SetWins(setString);

                var match = new Match
                {
                    TournamentId = vm.TournamentId,   // ⬅️
                    Player1Id = vm.Player1Id,
                    Player2Id = vm.Player2Id,
                    DatePlayed = vm.DatePlayed,
                    SetScores = setString,
                    Score1 = s1,
                    Score2 = s2
                };

                _ctx.Matches.Add(match);
                await _ctx.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            /* ponowny return z dropdownami */
            ViewBag.Players = new SelectList(_ctx.Players, "Id", "Name");
            ViewBag.Tournaments = new SelectList(_ctx.Tournaments, "Id", "Name");
            return View(vm);
        }

        /* ==============================================================
           EDIT
        =================================================================*/

        // GET
        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null) return NotFound();

            var m = await _ctx.Matches.FindAsync(id);
            if (m is null) return NotFound();

            var vm = new MatchViewModel(m);           // pomocniczy ctor => wypełnia pola
            ViewBag.Players = new SelectList(_ctx.Players, "Id", "Name");
            ViewBag.Tournaments = new SelectList(_ctx.Tournaments, "Id", "Name", m.TournamentId);
            return View(vm);
        }

        // POST
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MatchViewModel vm)
        {
            if (id != vm.Id) return NotFound();

            if (!ModelState.IsValid)                   // uproszczona walidacja
            {
                ViewBag.Players = new SelectList(_ctx.Players, "Id", "Name");
                ViewBag.Tournaments = new SelectList(_ctx.Tournaments, "Id", "Name", vm.TournamentId);
                return View(vm);
            }

            var m = await _ctx.Matches.FindAsync(id);
            if (m is null) return NotFound();

            m.TournamentId = vm.TournamentId;
            m.Player1Id = vm.Player1Id;
            m.Player2Id = vm.Player2Id;
            m.DatePlayed = vm.DatePlayed;
            m.SetScores = string.Join(";", vm.Sets.Select(s => $"{s.Score1}:{s.Score2}"));
            (m.Score1, m.Score2) = SetWins(m.SetScores);

            _ctx.Update(m);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        /* ---------- DELETE (bez zmian) ---------- */
        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null) return NotFound();
            var m = await _ctx.Matches
                              .Include(x => x.Player1)
                              .Include(x => x.Player2)
                              .Include(x => x.Tournament)
                              .FirstOrDefaultAsync(x => x.Id == id);
            return m is null ? NotFound() : View(m);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var m = await _ctx.Matches.FindAsync(id);
            if (m is not null) _ctx.Matches.Remove(m);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        
        // ---------- PLAYERS ENTER SCORE ----------
        [Authorize(Roles = "Player,Organizer,Admin")]
        public async Task<IActionResult> EnterScore(int id)
        {
            var match = await _ctx.Matches
                .Include(m => m.Tournament)           // potrzebne do flagi
                .Include(m => m.Player1)
                .Include(m => m.Player2)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match is null) return NotFound();

            // ★ sprawdź, czy turniej w ogóle na to pozwala
            if (!match.Tournament!.AllowPlayersEnterScores &&
                User.IsInRole("Player"))
                return Forbid();

            // ★ czy zalogowany User ↔ Player1 / Player2 ?
            if (User.IsInRole("Player") && !await CurrentUserPlaysIn(match))
                return Forbid();

            // pokaż formularz
            var totalSets = match.Tournament.SetsToWin * 2 - 1;
            var vm = new MatchViewModel(match, includeScores: true);
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Player,Organizer,Admin")]
        public async Task<IActionResult> EnterScore(int id, MatchViewModel vm)
        {
            var match = await _ctx.Matches
                .Include(m => m.Tournament)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (match is null) return NotFound();

            // ponowne kontrole jak wyżej
            if (!match.Tournament!.AllowPlayersEnterScores &&
                !(User.IsInRole("Admin") || User.IsInRole("Organizer")))
                return Forbid();
            if (User.IsInRole("Player") && !await CurrentUserPlaysIn(match))
                return Forbid();
            if (match.IsApproved && User.IsInRole("Player"))
            {
                TempData["Err"] = "Wynik został już zatwierdzony.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!ModelState.IsValid)
            {
                // zwróć formularz z błędami
                return View(vm);
            }

            /* -------- zapis -------- */
            // Walidacja formatów setów (np. 11:9, 13:11)
            int pointsToWin = match.Tournament!.PointsPerSet;

            foreach (var set in vm.Sets.Where(s => s.Score1.HasValue && s.Score2.HasValue))
            {
                var p1 = set.Score1.Value;
                var p2 = set.Score2.Value;
                int max = Math.Max(p1, p2);
                int min = Math.Min(p1, p2);

                // minimalna liczba punktów i przewaga 2 punktów
                if (max < pointsToWin || (max - min) < 2)
                {
                    ModelState.AddModelError("", $"Set {p1}:{p2} jest nieprawidłowy – trzeba zdobyć co najmniej {pointsToWin} punkty i wygrać różnicą 2 punktów.");
                    return View(vm);
                }
                // maksymalna liczba punktów bez przewagi – nie wolno mieć np. 14:4
                if (max > pointsToWin && (max - min) > 2)
                {
                    ModelState.AddModelError("", $"Set {p1}:{p2} jest nieprawidłowy – jeśli przekraczasz {pointsToWin} punktów, dozwolone są tylko wyniki na przewagi, np. {pointsToWin + 1}:{pointsToWin - 1}.");
                    return View(vm);
                }

                // nie może być remisu
                if (p1 == p2)
                {
                    ModelState.AddModelError("", $"Set {p1}:{p2} – wynik nie może być remisem.");
                    return View(vm);
                }
            }
            match.SetScores = vm.BuildSetScores();
            var (s1, s2) = SetWins(match.SetScores!);
            int setsToWin = match.Tournament!.SetsToWin;

            if ((s1 == setsToWin && s2 < setsToWin) || (s2 == setsToWin && s1 < setsToWin))
            {
                // OK – tylko jeden wygrał dokładnie X setów
            }
            else
            {
                ModelState.AddModelError("", $"Mecz powinien zakończyć się dokładnie {setsToWin} wygranymi setami jednego z graczy.");
                return View(vm);
            }
            
            match.Score1 = s1;
            match.Score2 = s2;
            match.DatePlayed = (vm.DatePlayed == default ? DateTime.Now : vm.DatePlayed);
            match.EnteredByUserId = _userManager.GetUserId(User); // potrzebny _userManager w ctorze
            match.IsApproved = User.IsInRole("Organizer") || User.IsInRole("Admin");


            await _ctx.SaveChangesAsync(); // zapisz najpierw mecz z IsApproved = true

            var tournament = await _ctx.Tournaments
                .Include(t => t.Matches)
                .Include(t => t.Players)
                .FirstOrDefaultAsync(t => t.Id == match.TournamentId);

            if (tournament != null && tournament.IsFinished && tournament.End == null)
            {
                tournament.End = DateTime.Now;
                await _ctx.SaveChangesAsync();
            }
            TempData["Msg"] = match.IsApproved ? "Wynik dodany." : "Wynik czeka na zatwierdzenie.";
            return RedirectToAction(nameof(Details), new { id });
        }

        /* ========== pomocnicze ========== */
        private async Task<bool> CurrentUserPlaysIn(Match m)
        {
            var uid = _userManager.GetUserId(User);
            return await _ctx.Players.AnyAsync(p =>
                   p.ApplicationUserId == uid &&
                   (p.Id == m.Player1Id || p.Id == m.Player2Id));
        }

        [Authorize(Roles = "Organizer,Admin")]
        public async Task<IActionResult> ReviewMatches(int tournamentId)
        {
            var tournament = await _ctx.Tournaments
                .Include(t => t.Matches)
                    .ThenInclude(m => m.Player1).ThenInclude(p => p.ApplicationUser)
                .Include(t => t.Matches)
                    .ThenInclude(m => m.Player2).ThenInclude(p => p.ApplicationUser)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament == null) return NotFound();

            if (User.IsInRole("Organizer") && tournament.CreatedById != _userManager.GetUserId(User))
                return Forbid();

            var matchesToReview = tournament.Matches
                .Where(m => !m.IsApproved && m.Player1Id != m.Player2Id)
                .OrderBy(m => m.RoundNumber)
                .ToList();

            return View(matchesToReview);
        }
        [Authorize(Roles = "Organizer,Admin")]
        public async Task<IActionResult> EditScore(int id)
        {
            var match = await _ctx.Matches
                .Include(m => m.Tournament)
                .Include(m => m.Player1).ThenInclude(p => p.ApplicationUser)
                .Include(m => m.Player2).ThenInclude(p => p.ApplicationUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match == null) return NotFound();

            if (User.IsInRole("Organizer") && match.Tournament.CreatedById != _userManager.GetUserId(User))
                return Forbid();

            return View(match);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditScore(int id, int score1, int score2, bool approve)
        {
            var match = await _ctx.Matches
                .Include(m => m.Tournament)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match == null) return NotFound();

            if (User.IsInRole("Organizer") && match.Tournament.CreatedById != _userManager.GetUserId(User))
                return Forbid();

            // Zakaz edycji starych rund w Knockout
            var latestRound = match.Tournament.Matches.Max(m => m.RoundNumber);
            if (match.Tournament.Type == CompetitionType.Knockout && match.RoundNumber < latestRound)
            {
                TempData["Err"] = "Nie można edytować wyników z poprzednich rund.";
                return RedirectToAction("Details", "Tournaments", new { id = match.TournamentId });
            }

            match.Score1 = score1;
            match.Score2 = score2;
            if (match.DatePlayed == null && (match.Score1 + match.Score2) > 0)
                match.DatePlayed = DateTime.Now;
            match.IsApproved = approve;

            await _ctx.SaveChangesAsync();
            TempData["Msg"] = "Wynik został zapisany.";
            return RedirectToAction("Details", "Tournaments", new { id = match.TournamentId });
        }
        [HttpPost]
        [Authorize(Roles = "Organizer,Admin")]
        public async Task<IActionResult> ApproveMatch(int id)
        {
            var match = await _ctx.Matches
                .Include(m => m.Tournament)
                    .ThenInclude(t => t.Matches)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match == null) return NotFound();

            if (User.IsInRole("Organizer") && match.Tournament.CreatedById != _userManager.GetUserId(User))
                return Forbid();

            match.IsApproved = true;
            await _ctx.SaveChangesAsync(); // najpierw zapisz zmianę (zatwierdzenie)

            var tournament = await _ctx.Tournaments
                .Include(t => t.Matches)
                .Include(t => t.Players)
                .FirstOrDefaultAsync(t => t.Id == match.TournamentId);

            if (tournament != null && tournament.IsFinished && tournament.End == null)
            {
                tournament.End = DateTime.Now;
                await _ctx.SaveChangesAsync();
            }
           

            TempData["Msg"] = "Mecz został zatwierdzony.";
            return RedirectToAction("ReviewMatches", new { tournamentId = match.TournamentId });
        }


    }
}
