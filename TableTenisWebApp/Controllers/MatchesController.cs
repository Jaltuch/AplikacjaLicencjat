using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TableTenisWebApp.Data;
using TableTenisWebApp.Models;
using TableTenisWebApp.Models.ViewModels;

namespace TableTenisWebApp.Controllers
{
    [Authorize(Roles = "Organizer,Admin")]
    public class MatchesController : Controller
    {
        private (int, int) CalculateSetWins(string setScores)
        {
            int player1Wins = 0;
            int player2Wins = 0;
            if (string.IsNullOrEmpty(setScores)) return (0, 0);

            var sets = setScores.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var set in sets)
            {
                var scores = set.Split(':');
                if (scores.Length == 2 && int.TryParse(scores[0], out int s1) && int.TryParse(scores[1], out int s2))
                {
                    if (s1 > s2) player1Wins++;
                    else if (s2 > s1) player2Wins++;
                }
            }
            return (player1Wins, player2Wins);
        }

        private readonly AppDbContext _context;

        public MatchesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Matches
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Matches.Include(m => m.Player1).Include(m => m.Player2);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Matches/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var match = await _context.Matches
                .Include(m => m.Player1)
                .Include(m => m.Player2)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (match == null)
            {
                return NotFound();
            }

            return View(match);
        }

        // GET: Matches/Create
        public IActionResult Create(int numberOfSets = 5)
        {
            var vm = new MatchViewModel
            {
                NumberOfSets = numberOfSets,
                Sets = Enumerable.Range(0, numberOfSets).Select(_ => new SetInput()).ToList(),
                DatePlayed = DateTime.Now
            };
            ViewBag.Players = new SelectList(_context.Players, "Id", "Name");
            return View(vm);
        }

        // POST: Matches/Create
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MatchViewModel vm)
        {
            if (vm.Player1Id == vm.Player2Id)
            {
                ModelState.AddModelError("", "Zawodnik nie może grać meczu przeciwko samemu sobie.");
            }
            // Sprawdź, czy wpisano jakiekolwiek sety
            if (vm.Sets == null || !vm.Sets.Any(s => s.Score1.HasValue && s.Score2.HasValue))
            {
                ModelState.AddModelError("", "Musisz wpisać wyniki przynajmniej jednego seta.");
            }
            // Policz liczbę wygranych setów dla obu zawodników
            var player1Wins = vm.Sets.Count(s => s.Score1 > s.Score2);
            var player2Wins = vm.Sets.Count(s => s.Score2 > s.Score1);
            var setsToWin = (vm.NumberOfSets / 2) + 1;

            //   Sprawdź, czy ktoś wygrał prawidłową liczbę setów
            if (player1Wins < setsToWin && player2Wins < setsToWin)
            {
                ModelState.AddModelError("", $"Mecz musi się zakończyć zwycięstwem jednego zawodnika ({setsToWin} wygranych setów).");
            }
            if (player1Wins >= setsToWin && player2Wins >= setsToWin)
            {
                ModelState.AddModelError("", "Obaj zawodnicy nie mogą mieć po tyle samo wygranych setów.");
            }

            //  Poprawność wyników liczbowo
            foreach (var set in vm.Sets)
            {
                if (!set.Score1.HasValue || !set.Score2.HasValue)
                    continue;
                if (set.Score1 < 0 || set.Score2 < 0)
                    ModelState.AddModelError("", "Wynik seta nie może być ujemny.");
            }


            if (ModelState.IsValid)
            {
                // Zbuduj string z wynikami setów
                var setScores = string.Join(";", vm.Sets.Select(s => $"{s.Score1}:{s.Score2}"));

                var (w1, w2) = CalculateSetWins(setScores);
                var match = new Match
                {
                    Player1Id = vm.Player1Id,
                    Player2Id = vm.Player2Id,
                    DatePlayed = vm.DatePlayed,
                    SetScores = setScores,
                    Score1 = w1,
                    Score2 = w2
                };
                _context.Add(match);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Players = new SelectList(_context.Players, "Id", "Name");
            return View(vm);
        }

        // GET: Matches/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var match = await _context.Matches.FindAsync(id);
            if (match == null)
            {
                return NotFound();
            }
            
            
            ViewBag.Players = new SelectList(_context.Players, "Id", "Name", match.Player1Id);
            return View(match);
        }

        // POST: Matches/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Player1Id,Player2Id,SetScores,DatePlayed")] Match match)
        {
            if (!string.IsNullOrEmpty(match.SetScores))
            {
                var (w1, w2) = CalculateSetWins(match.SetScores);
                match.Score1 = w1;
                match.Score2 = w2;
            }

            if (id != match.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(match);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MatchExists(match.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Players = new SelectList(_context.Players, "Id", "Name", match.Player1Id);
            return View(match);
        }

        // GET: Matches/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var match = await _context.Matches
                .Include(m => m.Player1)
                .Include(m => m.Player2)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (match == null)
            {
                return NotFound();
            }

            return View(match);
        }

        // POST: Matches/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var match = await _context.Matches.FindAsync(id);
            if (match != null)
            {
                _context.Matches.Remove(match);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MatchExists(int id)
        {
            return _context.Matches.Any(e => e.Id == id);
        }
    }
}
