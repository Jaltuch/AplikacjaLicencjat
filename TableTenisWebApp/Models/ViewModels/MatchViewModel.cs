using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TableTenisWebApp.Models;   // <- potrzebne do konstruktora

namespace TableTenisWebApp.Models.ViewModels
{
    public class MatchViewModel
    {
        /* ------------------ pola podstawowe ------------------ */
        public int? Id { get; set; }          // potrzebne przy Edit
        public int? TournamentId { get; set; }          // dropdown z turniejami
        public string? Player1Name { get; set; }
        public string? Player2Name { get; set; }
        [Required] public int Player1Id { get; set; }
        [Required] public int Player2Id { get; set; }

        public DateTime? DatePlayed { get; set; }

        /* ------------------ sety ------------------ */
        [Range(1, 7)]
        public int NumberOfSets { get; set; } = 5;

        public List<SetInput> Sets { get; set; } = new();

        /* ------------------ konstruktory ------------------ */
        public MatchViewModel() { }

        // uproszczone mapowanie z bazy -> view-model (dla Edit)
        public MatchViewModel(Match m, bool includeScores = false)
        {
            Id = m.Id;
            TournamentId = m.TournamentId;
            Player1Id = m.Player1Id;
            Player2Id = m.Player2Id;
            Player1Name = m.Player1?.Name;
            Player2Name = m.Player2?.Name;
            DatePlayed = m.DatePlayed;

            Sets = new List<SetInput>();
            // parsowanie setów
            if (includeScores && !string.IsNullOrWhiteSpace(m.SetScores))
            {
                var sets = m.SetScores.Split(';', StringSplitOptions.RemoveEmptyEntries);
                NumberOfSets = sets.Length;
                Sets = sets.Select(s =>
                {
                    var p = s.Split(':');
                    return new SetInput
                    {
                        Score1 = int.TryParse(p[0], out var a) ? a : (int?)null,
                        Score2 = int.TryParse(p[1], out var b) ? b : (int?)null
                    };
                }).ToList();
            }
            else
            {
                NumberOfSets = m.Tournament?.SetsToWin * 2 - 1 ?? 5;
                Sets = Enumerable.Range(0, NumberOfSets)
                                 .Select(_ => new SetInput())
                                 .ToList();
            }
        }
        public string BuildSetScores() =>
           string.Join(';', Sets.Select(s => $"{s.Score1 ?? 0}:{s.Score2 ?? 0}"));
    }

    public class SetInput
    {
        public int? Score1 { get; set; }
        public int? Score2 { get; set; }
    }
}
