using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using TableTenisWebApp.Data;
using TableTenisWebApp.Models;

namespace TableTenisWebApp.Controllers
{
    [Authorize(Roles = "Organizer,Admin")]          // domyślnie tylko org/admin
    public class TournamentsController : Controller
    {
        private readonly AppIdentityDbContext _ctx;

        public TournamentsController(AppIdentityDbContext ctx) => _ctx = ctx;

        // ========== PUBLIC LISTA + SZCZEGÓŁY ==========

        [AllowAnonymous]
        public async Task<IActionResult> Index()
            => View(await _ctx.Tournaments.AsNoTracking().ToListAsync());

        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return NotFound();

            var t = await _ctx.Tournaments
                .Include(x => x.Players).ThenInclude(tp => tp.Player)
                .Include(x => x.Matches).ThenInclude(m => m.Player1)
                .Include(x => x.Matches).ThenInclude(m => m.Player2)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            return t is null ? NotFound() : View(t);
        }

        // ========== CREATE ==========

        public IActionResult Create()
        {
            ViewBag.AllPlayers = _ctx.Players.ToList();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Tournament t,
            int[] selectedPlayers)
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

        // ========== EDIT ==========

        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null) return NotFound();

            var t = await _ctx.Tournaments
                .Include(x => x.Players)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (t is null) return NotFound();

            ViewBag.AllPlayers = _ctx.Players.ToList();
            ViewBag.SelectedIds = t.Players.Select(p => p.PlayerId).ToArray();
            return View(t);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Tournament t,
            int[] selectedPlayers)
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

            // aktualizacja pól prostych
            dbT.Name = t.Name;
            dbT.Start = t.Start;
            dbT.End = t.End;

            // synchronizacja many-to-many
            dbT.Players.Clear();
            foreach (var pid in selectedPlayers)
                dbT.Players.Add(new TournamentPlayer { PlayerId = pid });

            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ========== DELETE ==========

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
    }
}
