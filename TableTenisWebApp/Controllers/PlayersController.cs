using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TableTenisWebApp.Data;
using TableTenisWebApp.Models;
using TableTenisWebApp.Models.ViewModels;

namespace TableTenisWebApp.Controllers
{
    [Authorize]
    public class PlayersController : Controller
    {
        private readonly AppIdentityDbContext _context;
        private readonly UserManager<ApplicationUser> _um;

        public PlayersController(AppIdentityDbContext context, UserManager<ApplicationUser> um)
        {
            _context = context;
            _um = um;
        }

        // GET: Players
        public async Task<IActionResult> Index()
        {
            return View(await _context.Players.ToListAsync());
        }

        // GET: Players/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var player = await _context.Players
                .Include(p => p.ApplicationUser)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (player == null)
                return NotFound();

            // Pobierz mecze z udziałem zawodnika
            var matches = await _context.Matches
                .Include(m => m.Player1)
                .Include(m => m.Player2)
                .Include(m => m.Tournament)
                .Where(m => m.Player1Id == id || m.Player2Id == id)
                .OrderByDescending(m => m.DatePlayed)
                .ToListAsync();

            // Oblicz statystyki
            int wins = matches.Count(m =>
                (m.Player1Id == id && m.Score1 > m.Score2) ||
                (m.Player2Id == id && m.Score2 > m.Score1));

            int loses = matches.Count(m =>
                (m.Player1Id == id && m.Score1 < m.Score2) ||
                (m.Player2Id == id && m.Score2 < m.Score1));

            int setsWon = 0, setsLost = 0;
            foreach (var m in matches)
            {
                if (m.Player1Id == id)
                {
                    setsWon += m.Score1;
                    setsLost += m.Score2;
                }
                else
                {
                    setsWon += m.Score2;
                    setsLost += m.Score1;
                }
            }

            ViewBag.Wins = wins;
            ViewBag.Loses = loses;
            ViewBag.SetsWon = setsWon;
            ViewBag.SetsLost = setsLost;
            ViewBag.Matches = matches;

            return View(player);
        }

        // GET: Players/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Players/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] Player player)
        {
            if (ModelState.IsValid)
            {
                _context.Add(player);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(player);
        }

        // GET: Players/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var player = await _context.Players.FindAsync(id);
            if (player == null)
            {
                return NotFound();
            }
            // --- uprawnienie ---
            if (!User.IsInRole("Admin") &&
                player.ApplicationUserId != _um.GetUserId(User))
                return Forbid();   // HTTP 403

            return View(player);
        }

        // POST: Players/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,,ApplicationUserId")] Player player)
        {
            if (id != player.Id)
            {
                return NotFound();
            }
            // --- uprawnienie ---
            if (!User.IsInRole("Admin") &&
                player.ApplicationUserId != _um.GetUserId(User))
                return Forbid();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(player);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PlayerExists(player.Id))
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
            return View(player);
        }

        // GET: Players/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var player = await _context.Players
                .FirstOrDefaultAsync(m => m.Id == id);
            if (player == null)
            {
                return NotFound();
            }
            // --- uprawnienie ---
            if (!User.IsInRole("Admin") &&
                player.ApplicationUserId != _um.GetUserId(User))
                return Forbid();

            return View(player);
        }

        // POST: Players/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            
            var player = await _context.Players.FindAsync(id);
            // --- uprawnienie ---
            if (!User.IsInRole("Admin") &&
                player.ApplicationUserId != _um.GetUserId(User))
                return Forbid();

            if (player != null)
            {
                _context.Players.Remove(player);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PlayerExists(int id)
        {
            return _context.Players.Any(e => e.Id == id);
        }
        public async Task<IActionResult> Ranking()
        {
            var players = await _context.Players.ToListAsync();

            // Pobierz mecze
            var matches = await _context.Matches.ToListAsync();

            // Przygotuj ranking
            var ranking = players.Select(p =>
            {
                var asPlayer1 = matches.Where(m => m.Player1Id == p.Id);
                var asPlayer2 = matches.Where(m => m.Player2Id == p.Id);

                int played = asPlayer1.Count() + asPlayer2.Count();

                int wins =
                    asPlayer1.Count(m => m.Score1 > m.Score2) +
                    asPlayer2.Count(m => m.Score2 > m.Score1);

                int loses =
                    asPlayer1.Count(m => m.Score1 < m.Score2) +
                    asPlayer2.Count(m => m.Score2 < m.Score1);

                int setsWon =
                    asPlayer1.Sum(m => m.Score1) +
                    asPlayer2.Sum(m => m.Score2);

                int setsLost =
                    asPlayer1.Sum(m => m.Score2) +
                    asPlayer2.Sum(m => m.Score1);

                return new PlayerRankingViewModel
                {
                    Player = p,
                    Played = played,
                    Wins = wins,
                    Loses = loses,
                    SetsWon = setsWon,
                    SetsLost = setsLost
                };
            })
            .OrderByDescending(r => r.Wins)
            .ThenByDescending(r => r.SetsWon)
            .ThenBy(r => r.Loses)
            .ToList();

            return View(ranking);
        }





    }
}
