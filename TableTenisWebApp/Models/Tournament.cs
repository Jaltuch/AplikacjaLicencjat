using System.ComponentModel.DataAnnotations;

namespace TableTenisWebApp.Models
{
    public class Tournament
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = null!;
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }

        public CompetitionType Type { get; set; } = CompetitionType.Knockout;

        /// <summary> Ile setów trzeba wygrać, by wygrać mecz (np. 3&nbsp;→ BO5) </summary>
        [Range(1, 5)]
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
