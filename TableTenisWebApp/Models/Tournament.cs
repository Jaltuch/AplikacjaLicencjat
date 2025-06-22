namespace TableTenisWebApp.Models
{
    public class Tournament
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime Start { get; set; }
        public DateTime? End { get; set; }

        // nawigacje
        public ICollection<TournamentPlayer> Players { get; set; } = new List<TournamentPlayer>();
        public ICollection<Match> Matches { get; set; } = new List<Match>();
    }

}
