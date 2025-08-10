using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TableTenisWebApp.Models
{
    public class Tournament
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = null!;
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }

        [Range(2, 64)]
        public int? MaxPlayers { get; set; }
        
        public bool HasStarted { get; set; } = false;

        [NotMapped]
        public bool IsFinished
        {
            get
            {
                if (Type == CompetitionType.Knockout)
                {
                    if (Matches == null || Matches.Count == 0)
                        return false;

                    var lastRoundNumber = Matches.Max(m => m.RoundNumber);
                    var lastRoundMatches = Matches.Where(m => m.RoundNumber == lastRoundNumber).ToList();

                    // ✅ musi być dokładnie 1 mecz i to nie BYE
                    if (lastRoundMatches.Count == 1)
                    {
                        var final = lastRoundMatches.First();
                        return final.Player1Id != final.Player2Id &&
                               final.IsApproved &&
                               final.Score1 != final.Score2;
                    }

                    return false; // za dużo meczów lub BYE – jeszcze nie finał

                }

                if (Type == CompetitionType.League)
                {
                    var playerCount = Players?.Count ?? 0;

                    // Dodana walidacja: za mało graczy, nie można uznać za zakończony
                    if (playerCount < 2)
                        return false;

                    var expectedMatches = playerCount * (playerCount - 1) / 2;

                    var playedMatches = Matches?.Count(m => m.Player1Id != m.Player2Id) ?? 0;
                    var approvedMatches = Matches?.Count(m => m.Player1Id != m.Player2Id && m.IsApproved) ?? 0;

                    return playedMatches == expectedMatches && approvedMatches == expectedMatches;
                }

                return false;
            }
        }

        [NotMapped]
        public bool IsOpenForSignup
        {
            get
            {
                return !HasStarted
                    && !IsFinished
                    && (MaxPlayers == null || Players?.Count < MaxPlayers);
            }
        }





        public string? CreatedById { get; set; }
        public ApplicationUser? CreatedBy { get; set; }

        public CompetitionType Type { get; set; } = CompetitionType.Knockout;

        /// <summary> Ile setów trzeba wygrać, by wygrać mecz (np. 3&nbsp;→ BO5) </summary>
        [Range(2, 5)]
        public int SetsToWin { get; set; } = 3;

        /// <summary> Liczba punktów w secie (zwykle 11).</summary>
        [Range(5, 21)]
        public int PointsPerSet { get; set; } = 11;

        /// <summary> Czy gracze mogą samodzielnie wpisywać wyniki w tym turnieju. </summary>
        public bool AllowPlayersEnterScores { get; set; } = false;


        // nawigacje
        public ICollection<TournamentPlayer> Players { get; set; } = new List<TournamentPlayer>();
        public ICollection<Match> Matches { get; set; } = new List<Match>();
    }

}
