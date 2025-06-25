using TableTenisWebApp.Models;
using Microsoft.EntityFrameworkCore;
using TableTenisWebApp.Data;

namespace TableTenisWebApp.Services;

public class KnockoutService
{
    private readonly AppIdentityDbContext _ctx;
    private readonly Random _rand = new();

    public KnockoutService(AppIdentityDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<bool> GenerateFirstRoundAsync(Tournament t)
    {
        var players = t.Players.Select(tp => tp.Player).OrderBy(_ => _rand.Next()).ToList();
        int playerCount = players.Count;
        if (playerCount < 2) return false;

        int nextPowerOfTwo = (int)Math.Pow(2, Math.Ceiling(Math.Log2(playerCount)));
        int byeCount = nextPowerOfTwo - playerCount;

        var advancing = new List<Player>();

        // Dodaj BYE (gracze przechodzą automatycznie)
        for (int i = 0; i < byeCount; i++)
            advancing.Add(players[i]);

        // Reszta gra w parach
        for (int i = byeCount; i < players.Count; i += 2)
        {
            var p1 = players[i];
            var p2 = players[i + 1];
            t.Matches.Add(new Match
            {
                Player1Id = p1.Id,
                Player2Id = p2.Id,
                Tournament = t,
                RoundNumber = 1,
                DatePlayed = DateTime.Today
            });
        }

        // Gracze z BYE przechodzą do kolejnej rundy jako zwycięzcy "fikcyjnych" meczów
        foreach (var p in advancing)
        {
            t.Matches.Add(new Match
            {
                Player1Id = p.Id,
                Player2Id = p.Id,
                Score1 = 1,
                Score2 = 0,
                IsApproved = true,
                Tournament = t,
                RoundNumber = 1,
                DatePlayed = DateTime.Today
            });
        }

        await _ctx.SaveChangesAsync();
        return true;
    }

    public async Task<bool> GenerateNextRoundAsync(Tournament t)
    {
        int currentMaxRound = t.Matches.Max(m => m.RoundNumber);

        // wybierz zwycięzców z poprzedniej rundy
        var lastRoundMatches = t.Matches
            .Where(m => m.RoundNumber == currentMaxRound && m.IsApproved)
            .ToList();

        var winners = lastRoundMatches
            .Select(m => m.Score1 > m.Score2 ? m.Player1Id : m.Player2Id)
            .Distinct()
            .ToList();

        if (winners.Count < 2) return false; // koniec turnieju

        winners = winners.OrderBy(_ => _rand.Next()).ToList();
        int total = winners.Count;

        bool hasBye = total % 2 == 1;
        int nextRound = currentMaxRound + 1;

        for (int i = 0; i < total - 1; i += 2)
        {
            t.Matches.Add(new Match
            {
                Player1Id = winners[i],
                Player2Id = winners[i + 1],
                RoundNumber = nextRound,
                Tournament = t,
                DatePlayed = DateTime.Today
            });
        }

        if (hasBye)
        {
            // ostatni gracz dostaje BYE
            int p = winners[^1];
            t.Matches.Add(new Match
            {
                Player1Id = p,
                Player2Id = p,
                Score1 = 1,
                Score2 = 0,
                IsApproved = true,
                RoundNumber = nextRound,
                Tournament = t,
                DatePlayed = DateTime.Today
            });
        }

        await _ctx.SaveChangesAsync();
        return true;
    }
}
